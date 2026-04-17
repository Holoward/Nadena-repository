using Domain.Common;

namespace Domain.Entities;

public class EmailLog : AuditableBaseEntityGuid
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Logged";
}
