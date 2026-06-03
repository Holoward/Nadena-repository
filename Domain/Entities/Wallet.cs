using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Tracks a contributor's or platform's balance. PendingBalance holds amounts credited but not yet paid out via PayPal. Balance holds amounts available for withdrawal.
/// </summary>
public class Wallet : AuditableBaseEntityGuid
{
    public string OwnerType { get; set; } = "User"; // User | Platform
    public string OwnerId { get; set; } = string.Empty; // userId or "platform"
    public decimal Balance { get; set; }
    public decimal PendingBalance { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
