namespace Application.Interfaces;

/// <summary>
/// Abstraction over payment splitting/distribution.
/// The local implementation records VolunteerPayment rows directly in the DB.
/// swap for a real blockchain/smart-contract implementation when ready.
/// </summary>
public interface IBlockchainService
{
    /// <summary>
    /// Distributes revenue from a license purchase to volunteers proportionally.
    /// </summary>
    /// <param name="dataLicenseId">The license being paid for</param>
    /// <param name="dataPoolId">The pool whose volunteer contributors receive the split</param>
    /// <param name="totalAmount">Total amount paid by the buyer</param>
    /// <param name="volunteerSharePercent">Percentage that goes to volunteers (70-80)</param>
    /// <returns>A transaction/reference string for the distribution event</returns>
    Task<string> DistributeRevenueAsync(
        Guid dataLicenseId,
        int dataPoolId,
        decimal totalAmount,
        decimal volunteerSharePercent);
}
