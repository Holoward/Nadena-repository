using Domain.Common;

namespace Domain.Entities;

public class ContributorEmail : AuditableBaseEntity
{
    public string ContributorId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}