using Domain.Common;

namespace Domain.Entities;

public class Wallet : AuditableBaseEntityGuid
{
    public string OwnerType { get; set; } = "User"; // User | Platform
    public string OwnerId { get; set; } = string.Empty; // userId or "platform"
    public decimal Balance { get; set; }
    public decimal PendingBalance { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
