using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Data Contributor")]
/// <summary>
/// Handles contributor data submission. Accepts Google Takeout ZIP uploads, validates them, forwards anonymized payload to buyer endpoints, and credits the contributor wallet.
/// </summary>
public class TakeoutController : ControllerBase
{
    private readonly ITakeoutValidationService _validationService;
    private readonly IDataDeliveryService _deliveryService;
    private readonly IVolunteerRepository _volunteerRepository;
    private readonly IWatchEventRepository _watchEventRepository;
    private readonly IRepositoryAsync<DatasetPurchase> _purchaseRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TakeoutController> _logger;

    /// <summary>
    /// Creates the controller with validation, delivery, repository, wallet, user, and logging services.
    /// </summary>
    public TakeoutController(
        ITakeoutValidationService validationService,
        IDataDeliveryService deliveryService,
        IVolunteerRepository volunteerRepository,
        IWatchEventRepository watchEventRepository,
        IRepositoryAsync<DatasetPurchase> purchaseRepository,
        IWalletRepository walletRepository,
        ICurrentUserService currentUserService,
        ILogger<TakeoutController> logger)
    {
        _validationService = validationService;
        _deliveryService = deliveryService;
        _volunteerRepository = volunteerRepository;
        _watchEventRepository = watchEventRepository;
        _purchaseRepository = purchaseRepository;
        _walletRepository = walletRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [EnableRateLimiting("upload")]
    [RequestSizeLimit(524_288_000)]
    [Consumes("multipart/form-data")]
    /// <summary>
    /// Accepts a contributor's Takeout ZIP, validates it, delivers anonymized data to active buyers, and credits the contributor wallet.
    /// </summary>
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile zipFile,
        [FromForm] string googleAccountEmail)
    {
        if (zipFile == null || zipFile.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        if (string.IsNullOrWhiteSpace(googleAccountEmail))
            return BadRequest(new { error = "Google account email is required." });

        var userId = _currentUserService.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var volunteer = await _volunteerRepository.GetByUserIdAsync(userId);
        if (volunteer == null)
            return NotFound(new { error = "Contributor record not found." });

        if (!string.IsNullOrWhiteSpace(volunteer.DataIntegrityHash)
            && volunteer.IntegrityStatus == IntegrityStatus.Verified)
        {
            return Conflict(new { error = "You have already submitted a verified export. Only one submission is accepted per account." });
        }

        volunteer.UploadAttempts += 1;
        volunteer.LastUploadAttempt = DateTime.UtcNow;
        await _volunteerRepository.UpdateAsync(volunteer);

        // Pass stream, not IFormFile, to keep the service layer ASP.NET-free
        using var stream = zipFile.OpenReadStream();
        var result = await _validationService.ValidateAndExtractAsync(stream, googleAccountEmail);

        if (!result.IsValid)
        {
            volunteer.IntegrityStatus = IntegrityStatus.Flagged;
            volunteer.IntegrityReason = result.FailureReason;
            await _volunteerRepository.UpdateAsync(volunteer);
            return UnprocessableEntity(new { error = result.FailureReason });
        }

        volunteer.DataIntegrityHash = result.GoogleAccountIdHash;
        volunteer.IntegrityStatus = IntegrityStatus.Verified;
        volunteer.IntegrityReason = "Passed all validation checks";
        volunteer.HasDonated = true;
        volunteer.ActivatedDate = DateTime.UtcNow;
        await _volunteerRepository.UpdateAsync(volunteer);

        var watchEvents = new List<Domain.Entities.WatchEvent>();
        var sessionId = 1;
        var positionInSession = 1;
        DateTime? lastEventTime = null;

        foreach (var kvp in result.Payload!.HourOfDayDistribution.OrderBy(k => k.Key))
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                var approxTime = result.Payload.EarliestRecord.AddHours(kvp.Key);

                if (lastEventTime.HasValue && (approxTime - lastEventTime.Value).TotalMinutes > 30)
                {
                    sessionId++;
                    positionInSession = 1;
                }

                watchEvents.Add(new Domain.Entities.WatchEvent
                {
                    ContributorId = Guid.Parse(userId),
                    VideoIdHash = string.Empty,
                    ChannelIdHash = string.Empty,
                    Category = result.Payload.CategoryDistribution.OrderByDescending(c => c.Value).FirstOrDefault().Key ?? "Other",
                    WatchedAt = approxTime,
                    HourOfDay = kvp.Key,
                    DayOfWeek = (int)approxTime.DayOfWeek,
                    Month = approxTime.Month,
                    Year = approxTime.Year,
                    IsRepeat = false,
                    SessionId = sessionId,
                    PositionInSession = positionInSession++,
                    Created = DateTime.UtcNow,
                    CreatedBy = userId
                });

                lastEventTime = approxTime;
            }
        }

        if (watchEvents.Count > 0)
        {
            await _watchEventRepository.ReplaceForContributorAsync(
                Guid.Parse(userId), watchEvents);
            _logger.LogInformation("Wrote {Count} WatchEvents for contributor {UserId}", watchEvents.Count, userId);
        }

        var allPurchases = await _purchaseRepository.ListAsync();
        var activePurchases = allPurchases
            .Where(p => p.Status == "Processing"
                     && !string.IsNullOrWhiteSpace(p.DeliveryEndpoint))
            .ToList();

        foreach (var purchase in activePurchases)
        {
            var delivery = await _deliveryService.ForwardAsync(
                result.Payload!,
                purchase.DeliveryEndpoint!,
                purchase.Id);

            if (delivery.Success)
                _logger.LogInformation("Delivered to purchase {PurchaseId}", purchase.Id);
            else
                _logger.LogWarning("Delivery failed for purchase {PurchaseId}: {Error}", purchase.Id, delivery.ErrorMessage);
        }

        var wallet = await _walletRepository.GetByOwnerAsync(userId);
        if (wallet == null)
        {
            wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                OwnerType = "User",
                OwnerId = userId,
                Balance = 0m,
                PendingBalance = 0.10m,
                Currency = "USD",
                LastUpdated = DateTime.UtcNow,
                Created = DateTime.UtcNow,
                CreatedBy = "System"
            };
            await _walletRepository.AddAsync(wallet);
        }
        else
        {
            wallet.PendingBalance += 0.10m;
            wallet.LastUpdated = DateTime.UtcNow;
            await _walletRepository.UpdateAsync(wallet);
        }

        return Ok(new
        {
            message = "Export received and verified. Your wallet has been credited.",
            totalWatchEvents = result.Payload!.TotalWatchEvents,
            dataSourceTypes = result.Payload.DataSourceTypes
        });
    }
}
