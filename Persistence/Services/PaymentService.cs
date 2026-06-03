using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Models;

namespace Persistence.Services;

public class PaymentService : IPaymentService
{
    private readonly NadenaIdentityDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;

    public PaymentService(
        NadenaIdentityDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _emailService = emailService;
        _auditLogService = auditLogService;
    }

    public async Task<ServiceResponse<string>> ProcessPurchaseAsync(Guid dataClientUserId, Guid datasetId, decimal price, string billingType, string idempotencyKey, bool contributorShareNow)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new ServiceResponse<string>("Missing idempotency key.");
        }

        var existing = await _dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);

        if (existing != null)
        {
            return new ServiceResponse<string>(existing.Id.ToString(), "Idempotent replay");
        }

        var buyerUser = await _userManager.FindByIdAsync(dataClientUserId.ToString());
        if (buyerUser == null)
        {
            return new ServiceResponse<string>("Data client not found.");
        }

        if (price < 0)
        {
            return new ServiceResponse<string>("Invalid price.");
        }

        var platformWallet = await EnsureWalletAsync("Platform", "platform");
        var clientWallet = await EnsureWalletAsync("User", buyerUser.Id);

        if (clientWallet.Balance < price)
        {
            return new ServiceResponse<string>("Insufficient funds.");
        }

        var contributorCount = await _dbContext.Volunteers
            .CountAsync(v => v.IntegrityStatus != IntegrityStatus.Flagged);

        if (contributorCount == 0)
        {
            return new ServiceResponse<string>("No verified data contributors are available for this purchase.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var purchaseTransaction = new LedgerTransaction
            {
                Id = Guid.NewGuid(),
                FromWalletId = clientWallet.Id,
                ToWalletId = platformWallet.Id,
                Amount = price,
                Currency = "USD",
                Type = "DataPurchase",
                Status = "Completed",
                ReferenceId = datasetId.ToString(),
                ReferenceType = billingType,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                IdempotencyKey = idempotencyKey
            };

            var platformAmount = Math.Round(price * 0.30m, 2, MidpointRounding.AwayFromZero);
            var contributorAmount = price - platformAmount;

            var platformFeeTransaction = new LedgerTransaction
            {
                Id = Guid.NewGuid(),
                FromWalletId = clientWallet.Id,
                ToWalletId = platformWallet.Id,
                Amount = platformAmount,
                Currency = "USD",
                Type = "PlatformFee",
                Status = "Completed",
                ReferenceId = datasetId.ToString(),
                ReferenceType = "DatasetPurchase",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                IdempotencyKey = $"{idempotencyKey}:platform"
            };

            _dbContext.Transactions.Add(purchaseTransaction);
            _dbContext.Transactions.Add(platformFeeTransaction);

            clientWallet.Balance -= price;
            platformWallet.Balance += platformAmount;
            clientWallet.LastUpdated = DateTime.UtcNow;
            platformWallet.LastUpdated = DateTime.UtcNow;

            var contributorSplit = Math.Round(contributorAmount / contributorCount, 2, MidpointRounding.AwayFromZero);
            var remainder = contributorAmount - (contributorSplit * contributorCount);

            // Load into a List first. Npgsql does not support MARS (Multiple Active Result
            // Sets): keeping an open streaming cursor while calling SaveChangesAsync() on
            // the same DbContext connection throws "A command is already in progress".
            var contributors = await _dbContext.Volunteers
                .Where(v => v.IntegrityStatus != IntegrityStatus.Flagged)
                .OrderBy(v => v.Id)
                .ToListAsync();

            int index = 0;
            foreach (var contributor in contributors)
            {
                var contributorWallet = await EnsureWalletAsync("User", contributor.UserId);
                var amount = index == 0 ? contributorSplit + remainder : contributorSplit;
                var payoutStatus = contributorShareNow ? "Completed" : "Held";

                var payoutTransaction = new LedgerTransaction
                {
                    Id = Guid.NewGuid(),
                    FromWalletId = platformWallet.Id,
                    ToWalletId = contributorWallet.Id,
                    Amount = amount,
                    Currency = "USD",
                    Type = "ContributorPayout",
                    Status = payoutStatus,
                    ReferenceId = datasetId.ToString(),
                    ReferenceType = "DatasetPurchase",
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = contributorShareNow ? DateTime.UtcNow : null,
                    IdempotencyKey = $"{idempotencyKey}:contributor:{contributor.UserId}"
                };

                _dbContext.Transactions.Add(payoutTransaction);

                if (contributorShareNow)
                {
                    contributorWallet.Balance += amount;
                }
                else
                {
                    contributorWallet.PendingBalance += amount;
                }

                contributorWallet.LastUpdated = DateTime.UtcNow;

                var contributorUser = await _userManager.FindByIdAsync(contributor.UserId);
                if (contributorUser?.Email is { Length: > 0 } contributorEmail)
                {
                    await _emailService.SendDataPurchasedAsync(contributorEmail, contributorUser.FullName, amount);
                }

                index++;
                if (index % 500 == 0)
                {
                    await _dbContext.SaveChangesAsync();
                }
            }

            if (index % 500 != 0)
            {
                await _dbContext.SaveChangesAsync();
            }
            await transaction.CommitAsync();

            await _emailService.SendClientPurchaseConfirmedAsync(buyerUser.Email ?? buyerUser.UserName ?? "client@nadena.local", datasetId.ToString(), price);
            await _emailService.SendAdminPlatformFeeAsync(platformAmount);
            await _auditLogService.LogAsync("DataPurchased", "LedgerTransaction", purchaseTransaction.Id.ToString(), true, buyerUser.Id, newValues: $"{{\"amount\":{price},\"billingType\":\"{billingType}\"}}");

            return new ServiceResponse<string>(purchaseTransaction.Id.ToString(), "Purchase processed");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            await _auditLogService.LogAsync("DataPurchaseFailed", "LedgerTransaction", datasetId.ToString(), false, buyerUser.Id, errorMessage: ex.Message);
            return new ServiceResponse<string>($"Purchase failed: {ex.Message}");
        }
    }

    public async Task<ServiceResponse<string>> ReleaseHeldPayoutAsync(Guid transactionId, string approvedByUserId)
    {
        var heldTransaction = await _dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId);
        if (heldTransaction == null)
        {
            return new ServiceResponse<string>("Held payout not found.");
        }

        if (!string.Equals(heldTransaction.Status, "Held", StringComparison.OrdinalIgnoreCase))
        {
            return new ServiceResponse<string>("Only held payouts can be released.");
        }

        var contributorWallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.Id == heldTransaction.ToWalletId);
        if (contributorWallet == null)
        {
            return new ServiceResponse<string>("Contributor wallet not found.");
        }

        var completionTransaction = new LedgerTransaction
        {
            Id = Guid.NewGuid(),
            FromWalletId = heldTransaction.FromWalletId,
            ToWalletId = heldTransaction.ToWalletId,
            Amount = heldTransaction.Amount,
            Currency = heldTransaction.Currency,
            Type = heldTransaction.Type,
            Status = "Completed",
            ReferenceId = heldTransaction.Id.ToString(),
            ReferenceType = "HeldPayoutApproval",
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            IdempotencyKey = $"release:{heldTransaction.Id}"
        };

        contributorWallet.PendingBalance = Math.Max(0m, contributorWallet.PendingBalance - heldTransaction.Amount);
        contributorWallet.Balance += heldTransaction.Amount;
        contributorWallet.LastUpdated = DateTime.UtcNow;

        _dbContext.Transactions.Add(completionTransaction);
        await _dbContext.SaveChangesAsync();

        var contributorUser = await _userManager.FindByIdAsync(contributorWallet.OwnerId);
        if (contributorUser?.Email is { Length: > 0 } contributorEmail)
        {
            await _emailService.SendPayoutProcessedAsync(contributorEmail, contributorUser.FullName, heldTransaction.Amount);
        }

        await _auditLogService.LogAsync("PayoutReleased", "LedgerTransaction", completionTransaction.Id.ToString(), true, approvedByUserId, newValues: $"{{\"sourceTransactionId\":\"{heldTransaction.Id}\"}}");
        return new ServiceResponse<string>(completionTransaction.Id.ToString(), "Held payout released.");
    }

    public async Task<ServiceResponse<string>> MarkDisbursedExternallyAsync(Guid transactionId, string adminUserId, string? notes = null)
    {
        var payoutTransaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.Type == "ContributorPayout");

        if (payoutTransaction == null)
        {
            return new ServiceResponse<string>("Contributor payout not found.");
        }

        var payoutWallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.Id == payoutTransaction.ToWalletId);
        if (payoutWallet == null)
        {
            return new ServiceResponse<string>("Contributor wallet not found.");
        }

        var alreadyRecorded = await _dbContext.ContributorDisbursements.AnyAsync(d => d.TransactionId == transactionId);
        if (alreadyRecorded)
        {
            return new ServiceResponse<string>("External disbursement already recorded.");
        }

        var disbursement = new ContributorDisbursement
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            UserId = payoutWallet.OwnerId,
            Amount = payoutTransaction.Amount,
            DisbursedAt = DateTime.UtcNow,
            DisbursedByUserId = adminUserId,
            Notes = notes
        };

        _dbContext.ContributorDisbursements.Add(disbursement);
        await _dbContext.SaveChangesAsync();

        var contributorUser = await _userManager.FindByIdAsync(payoutWallet.OwnerId);
        if (contributorUser?.Email is { Length: > 0 } contributorEmail)
        {
            await _emailService.SendPayoutProcessedAsync(contributorEmail, contributorUser.FullName, payoutTransaction.Amount);
        }

        await _auditLogService.LogAsync("PayoutDisbursedExternally", "ContributorDisbursement", disbursement.Id.ToString(), true, adminUserId, newValues: $"{{\"transactionId\":\"{transactionId}\",\"amount\":{payoutTransaction.Amount}}}");
        return new ServiceResponse<string>(disbursement.Id.ToString(), "External disbursement recorded.");
    }

    private async Task<Wallet> EnsureWalletAsync(string ownerType, string ownerId)
    {
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.OwnerId == ownerId);
        if (wallet != null)
        {
            return wallet;
        }

        wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            OwnerType = ownerType,
            OwnerId = ownerId,
            Currency = "USD",
            Balance = 0m,
            PendingBalance = 0m,
            LastUpdated = DateTime.UtcNow
        };

        _dbContext.Wallets.Add(wallet);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // Another concurrent request inserted the wallet first — fetch and return it
            _dbContext.ChangeTracker.Clear();
            wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.OwnerId == ownerId)
                ?? throw new InvalidOperationException($"Wallet for {ownerId} could not be created or retrieved.");
        }
        return wallet;
    }
}
