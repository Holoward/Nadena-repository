using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// A DAO data pool — a category of aggregated user data available for B2B licensing.
/// Examples: "YouTube Comment Behavior", "Fitness Tracker DAO", "Shopping Behavior DAO"
/// </summary>
public class DataPool : AuditableBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Category tag, e.g. "Social Media", "Health", "Commerce"</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Monthly licensing price in USD</summary>
    public decimal PricePerMonth { get; set; }

    /// <summary>Percentage of revenue that goes to volunteers (70-80). Platform keeps the rest.</summary>
    public decimal RevenueSharePercent { get; set; } = 75m;

    public bool IsActive { get; set; } = true;

    /// <summary>Approximate record count for buyers to evaluate pool size</summary>
    public long ApproximateRecordCount { get; set; }

    public string SourceTable { get; set; } = string.Empty;
}
