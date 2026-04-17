using Domain.Common;

namespace Domain.Entities;

public class Donation : AuditableBaseEntityGuid
{
    public string ContributorId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
}