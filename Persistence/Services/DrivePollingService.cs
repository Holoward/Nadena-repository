using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Persistence.Services;

public class DrivePollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DrivePollingService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);

    public DrivePollingService(
        IServiceScopeFactory scopeFactory,
        ILogger<DrivePollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DrivePollingService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAllContributorsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DrivePollingService encountered an error during polling.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task PollAllContributorsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var tokenRepo = scope.ServiceProvider.GetRequiredService<IContributorOAuthTokenRepository>();
        var volunteerRepo = scope.ServiceProvider.GetRequiredService<IVolunteerRepository>();
        var driveService = scope.ServiceProvider.GetRequiredService<IGoogleDriveService>();
        var validationService = scope.ServiceProvider.GetRequiredService<ITakeoutValidationService>();
        var deliveryService = scope.ServiceProvider.GetRequiredService<IDataDeliveryService>();
        var walletRepo = scope.ServiceProvider.GetRequiredService<IWalletRepository>();
        var purchaseRepo = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<DatasetPurchase>>();

        var activeTokens = await tokenRepo.GetAllActiveAsync();
        _logger.LogInformation("Polling {Count} contributors with active Drive tokens.", activeTokens.Count);

        foreach (var token in activeTokens)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var volunteer = await volunteerRepo.GetByUserIdAsync(token.ContributorId);

                if (volunteer == null || volunteer.HasDonated)
                    continue;

                string accessToken;
                try
                {
                    accessToken = await driveService.RefreshAccessTokenAsync(token.EncryptedRefreshToken);
                    token.AccessToken = accessToken;
                    token.AccessTokenExpiry = DateTime.UtcNow.AddHours(1);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Token refresh failed for contributor {Id}: {Error}",
                        token.ContributorId, ex.Message);
                    token.IsActive = false;
                    await tokenRepo.UpdateAsync(token);
                    continue;
                }

                var files = await driveService.FindTakeoutFilesAsync(accessToken, token.GrantedAt);

                if (!files.Any())
                {
                    token.LastPolledAt = DateTime.UtcNow;
                    await tokenRepo.UpdateAsync(token);
                    continue;
                }

                var latest = files.OrderByDescending(f => f.CreatedTime).First();
                _logger.LogInformation("Found Takeout file '{Name}' for contributor {Id}.",
                    latest.Name, token.ContributorId);

                Stream zipStream;
                try
                {
                    zipStream = await driveService.DownloadFileAsync(accessToken, latest.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Drive download failed for contributor {Id}: {Error}",
                        token.ContributorId, ex.Message);
                    continue;
                }

                using (zipStream)
                {
                    var result = await validationService.ValidateAndExtractAsync(
                        zipStream, token.ContributorId);

                    if (!result.IsValid)
                    {
                        _logger.LogWarning("Validation failed for contributor {Id}: {Reason}",
                            token.ContributorId, result.FailureReason);
                        volunteer.IntegrityStatus = IntegrityStatus.Flagged;
                        volunteer.IntegrityReason = result.FailureReason;
                        await volunteerRepo.UpdateAsync(volunteer);
                        continue;
                    }

                    volunteer.DataIntegrityHash = result.GoogleAccountIdHash;
                    volunteer.IntegrityStatus = IntegrityStatus.Verified;
                    volunteer.IntegrityReason = "Passed all validation checks (auto-polled)";
                    volunteer.HasDonated = true;
                    volunteer.ActivatedDate = DateTime.UtcNow;
                    await volunteerRepo.UpdateAsync(volunteer);

                    var allPurchases = await purchaseRepo.ListAsync();
                    var activePurchases = allPurchases
                        .Where(p => p.Status == "Processing"
                                 && !string.IsNullOrWhiteSpace(p.DeliveryEndpoint))
                        .ToList();

                    foreach (var purchase in activePurchases)
                    {
                        var delivery = await deliveryService.ForwardAsync(
                            result.Payload!, purchase.DeliveryEndpoint!, purchase.Id);

                        if (!delivery.Success)
                            _logger.LogWarning("Delivery failed for purchase {PurchaseId}: {Error}",
                                purchase.Id, delivery.ErrorMessage);
                    }

                    var wallet = await walletRepo.GetByOwnerAsync(token.ContributorId);
                    if (wallet == null)
                    {
                        await walletRepo.AddAsync(new Wallet
                        {
                            Id = Guid.NewGuid(),
                            OwnerType = "User",
                            OwnerId = token.ContributorId,
                            Balance = 0m,
                            PendingBalance = 0.10m,
                            Currency = "USD",
                            LastUpdated = DateTime.UtcNow,
                            Created = DateTime.UtcNow,
                            CreatedBy = "System"
                        });
                    }
                    else
                    {
                        wallet.PendingBalance += 0.10m;
                        wallet.LastUpdated = DateTime.UtcNow;
                        await walletRepo.UpdateAsync(wallet);
                    }

                    _logger.LogInformation("Auto-processed contributor {Id} successfully.", token.ContributorId);
                }

                token.LastPolledAt = DateTime.UtcNow;
                await tokenRepo.UpdateAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing contributor {Id}.", token.ContributorId);
            }
        }
    }
}
