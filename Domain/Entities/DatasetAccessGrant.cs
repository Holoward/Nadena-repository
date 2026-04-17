using Domain.Common;

namespace Domain.Entities;

public class DatasetAccessGrant : AuditableBaseEntityGuid
{
    public Guid DatasetPurchaseId { get; set; }
    public string GrantedByUserId { get; set; } = string.Empty;
    public string TeammateEmail { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
