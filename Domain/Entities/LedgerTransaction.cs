using Domain.Common;

namespace Domain.Entities;

public class LedgerTransaction : AuditableBaseEntityGuid
{
    public Guid FromWalletId { get; set; }
    public Guid ToWalletId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Type { get; set; } = "DataPurchase"; // DataPurchase | ContributorPayout | PlatformFee
    public string Status { get; set; } = "Pending"; // Pending | Completed | Failed | Held
    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? IdempotencyKey { get; set; }
}
