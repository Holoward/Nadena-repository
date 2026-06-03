using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Tracks active dataset subscriptions for recurring delivery
/// </summary>
public class DatasetSubscription : AuditableBaseEntity
{
    public Guid DatasetId { get; set; }
    public Guid BuyerId { get; set; }
    public string StripeSubscriptionId { get; set; } = string.Empty;
    public string PricingModel { get; set; } = "Monthly";
    public DateTime StartDate { get; set; }
    public DateTime? NextDeliveryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int RefreshCount { get; set; }
    public DateTime? LastDeliveredAt { get; set; }
}
