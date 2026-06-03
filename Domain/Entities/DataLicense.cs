using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A time-limited B2B license giving a buyer API access to a DataPool.
/// Replaces one-time DatasetPurchase for the B2B licensing model.
/// </summary>
public class DataLicense : AuditableBaseEntityGuid
{
    public string BuyerId { get; set; }
    public int DataPoolId { get; set; }
    public Guid ApiKeyId { get; set; }

    public DateTime LicensedFrom { get; set; }
    public DateTime LicensedUntil { get; set; }

    /// <summary>Total amount paid by the buyer in USD</summary>
    public decimal AmountPaid { get; set; }

    /// <summary>Platform fee retained (AmountPaid * (1 - RevenueSharePercent/100))</summary>
    public decimal PlatformFee { get; set; }

    /// <summary>Amount distributed to volunteers (AmountPaid - PlatformFee)</summary>
    public decimal VolunteerShare { get; set; }

    /// <summary>Number of months licensed</summary>
    public int MonthsLicensed { get; set; }

    public LicenseStatus Status { get; set; } = LicenseStatus.Active;

    /// <summary>Reference returned by IBlockchainService when revenue was distributed</summary>
    public string? DistributionTxRef { get; set; }
}
