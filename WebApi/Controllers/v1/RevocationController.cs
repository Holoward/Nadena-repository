using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace WebApi.Controllers.v1;

/// <summary>
/// Handles contributor consent revocation. Deletes all stored behavioral data,
/// notifies all buyers who received this contributor's data, and deactivates the OAuth token.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Data Contributor")]
public class RevocationController : ControllerBase
{
    private readonly IVolunteerRepository _volunteerRepository;
    private readonly IWatchEventRepository _watchEventRepository;
    private readonly IContributorOAuthTokenRepository _tokenRepository;
    private readonly IRepositoryAsync<Domain.Entities.DatasetPurchase> _purchaseRepository;
    private readonly IDataPoolRepository _poolRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RevocationController> _logger;

    public RevocationController(
        IVolunteerRepository volunteerRepository,
        IWatchEventRepository watchEventRepository,
        IContributorOAuthTokenRepository tokenRepository,
        IRepositoryAsync<Domain.Entities.DatasetPurchase> purchaseRepository,
        IDataPoolRepository poolRepository,
        ICurrentUserService currentUserService,
        IHttpClientFactory httpClientFactory,
        ILogger<RevocationController> logger)
    {
        _volunteerRepository = volunteerRepository;
        _watchEventRepository = watchEventRepository;
        _tokenRepository = tokenRepository;
        _purchaseRepository = purchaseRepository;
        _poolRepository = poolRepository;
        _currentUserService = currentUserService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Revokes consent for the current contributor.
    /// Deletes all WatchEvents, deactivates OAuth token, notifies buyers,
    /// and marks the volunteer as deleted.
    /// </summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeConsent()
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var volunteer = await _volunteerRepository.GetByUserIdAsync(userId);
        if (volunteer == null)
            return NotFound(new { error = "Contributor record not found." });

        var contributorGuid = Guid.TryParse(userId, out var g) ? g : Guid.Empty;

        // Step 1: Delete all WatchEvents for this contributor
        var watchEvents = await _watchEventRepository.GetByContributorIdAsync(contributorGuid);
        if (watchEvents.Any())
        {
            await _watchEventRepository.ReplaceForContributorAsync(contributorGuid, new List<Domain.Entities.WatchEvent>());
            _logger.LogInformation("Deleted {Count} WatchEvents for contributor {UserId}", watchEvents.Count, userId);
        }

        // Step 1b: Refresh ApproximateRecordCount on all pools backed by WatchEvents.
        await _poolRepository.RecalculateApproximateCountAsync("WatchEvents");

        // Step 2: Deactivate OAuth token
        var oauthToken = await _tokenRepository.GetByContributorIdAsync(userId);
        if (oauthToken != null)
        {
            oauthToken.IsActive = false;
            await _tokenRepository.UpdateAsync(oauthToken);

        }

        // Step 3: Notify all buyers who have an active delivery endpoint
        var allPurchases = await _purchaseRepository.ListAsync();
        var buyersToNotify = allPurchases
            .Where(p => !string.IsNullOrWhiteSpace(p.DeliveryEndpoint))
            .ToList();

        var client = _httpClientFactory.CreateClient();
        var notificationPayload = JsonSerializer.Serialize(new
        {
            type = "contributor_revocation",
            contributorHash = volunteer.DataIntegrityHash,
            revokedAt = DateTime.UtcNow.ToString("o"),
            message = "This contributor has revoked consent. Please delete all records associated with this contributor hash from your systems."
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        foreach (var purchase in buyersToNotify)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, purchase.DeliveryEndpoint)
                {
                    Content = new StringContent(notificationPayload, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-Nadena-Event", "contributor_revocation");
                request.Headers.Add("X-Nadena-Purchase-Id", purchase.Id.ToString());

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    _logger.LogInformation("Revocation notified to purchase {PurchaseId}", purchase.Id);
                else
                    _logger.LogWarning("Revocation notification failed for purchase {PurchaseId}: {Status}", purchase.Id, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify buyer for purchase {PurchaseId}", purchase.Id);
            }
        }

        // Step 4: Mark volunteer as deleted
        volunteer.Status = VolunteerStatus.Deleted;
        volunteer.HasDonated = false;
        volunteer.DataIntegrityHash = null;
        volunteer.IntegrityStatus = IntegrityStatus.Pending;
        await _volunteerRepository.UpdateAsync(volunteer);

        return Ok(new
        {
            message = "Your consent has been revoked. All behavioral data has been deleted from Nadena systems and buyers have been notified.",
            watchEventsDeleted = watchEvents.Count,
            buyersNotified = buyersToNotify.Count
        });
    }

    /// <summary>
    /// Returns the current revocation status for the contributor.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var volunteer = await _volunteerRepository.GetByUserIdAsync(userId);
        if (volunteer == null)
            return NotFound(new { error = "Contributor record not found." });

        var contributorGuid = Guid.TryParse(userId, out var g) ? g : Guid.Empty;
        var watchEventCount = (await _watchEventRepository.GetByContributorIdAsync(contributorGuid)).Count;
        var hasOAuthToken = (await _tokenRepository.GetByContributorIdAsync(userId))?.IsActive ?? false;

        return Ok(new
        {
            status = volunteer.Status.ToString(),
            hasVerifiedSubmission = volunteer.HasDonated,
            watchEventsStored = watchEventCount,
            googleDriveConnected = hasOAuthToken,
            canRevoke = volunteer.HasDonated && volunteer.Status != VolunteerStatus.Deleted
        });
    }
}
