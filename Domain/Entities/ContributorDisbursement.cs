using Domain.Common;

namespace Domain.Entities;

public class ContributorDisbursement : AuditableBaseEntityGuid
{
    public Guid TransactionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DisbursedAt { get; set; } = DateTime.UtcNow;
    public string DisbursedByUserId { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
