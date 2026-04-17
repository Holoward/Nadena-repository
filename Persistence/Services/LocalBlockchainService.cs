using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Services;

/// <summary>
/// Local (non-blockchain) implementation of IBlockchainService.
/// Distributes revenue to volunteers who contributed data to the licensed pool
/// by creating VolunteerPayment records proportionally.
///
/// To swap for a real smart contract: implement IBlockchainService in a new
/// class and replace this registration in ServiceExtension.cs.
/// </summary>
public class LocalBlockchainService : IBlockchainService
{
    private readonly ApplicationDbContext _dbContext;

    public LocalBlockchainService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> DistributeRevenueAsync(
        Guid dataLicenseId,
        int dataPoolId,
        decimal totalAmount,
        decimal volunteerSharePercent)
    {
        var volunteerShare = Math.Round(totalAmount * (volunteerSharePercent / 100m), 2);
        var platformFee = totalAmount - volunteerShare;

        // Find all volunteers who have commented (simplified - get all volunteers)
        // In a production system, this would track which volunteers contributed to which datasets/pools
        var contributingVolunteerIds = await _dbContext.Volunteers
            .Where(v => v.Status == VolunteerStatus.Activated)
            .Select(v => v.Id)
            .Distinct()
            .ToListAsync();

        if (contributingVolunteerIds.Count == 0)
        {
            // No volunteers yet — record as pending platform reserve
            return GenerateTxRef("platform_reserve", dataLicenseId);
        }

        // Split volunteer share equally among all contributors
        var sharePerVolunteer = Math.Round(volunteerShare / contributingVolunteerIds.Count, 2);
        var remainder = volunteerShare - (sharePerVolunteer * contributingVolunteerIds.Count);

        var txRef = GenerateTxRef("local_db", dataLicenseId);
        var payments = new List<VolunteerPayment>();

        for (int i = 0; i < contributingVolunteerIds.Count; i++)
        {
            var net = i == 0 ? sharePerVolunteer + remainder : sharePerVolunteer; // first gets any rounding remainder
            payments.Add(new VolunteerPayment
            {
                Id = Guid.NewGuid(),
                VolunteerId = contributingVolunteerIds[i],
                DatasetId = 0, // pool-level payment, not dataset-specific
                GrossAmount = totalAmount / contributingVolunteerIds.Count,
                PlatformFee = platformFee / contributingVolunteerIds.Count,
                NetAmount = net,
                Status = "Pending",
                PayPalBatchId = null,
                PayPalPayoutItemId = txRef
            });
        }

        await _dbContext.VolunteerPayments.AddRangeAsync(payments);
        await _dbContext.SaveChangesAsync();

        return txRef;
    }

    private static string GenerateTxRef(string type, Guid licenseId)
        => $"{type}_{licenseId:N}_{DateTime.UtcNow:yyyyMMddHHmmss}";
}
