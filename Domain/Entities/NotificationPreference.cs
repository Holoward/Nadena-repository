using Domain.Common;

namespace Domain.Entities;

public class NotificationPreference : AuditableBaseEntityGuid
{
    public string UserId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}
