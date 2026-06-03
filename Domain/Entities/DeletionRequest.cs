using Domain.Common;

namespace Domain.Entities;

public class DeletionRequest : AuditableBaseEntityGuid
{
    public string UserId { get; set; } = string.Empty;
    public int? VolunteerId { get; set; }
    public string Status { get; set; } = "Pending";
    public string Reason { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByUserId { get; set; }
    public string? ReviewNotes { get; set; }
}
