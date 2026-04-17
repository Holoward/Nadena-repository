using Domain.Common;

namespace Domain.Entities;

public class PasswordResetRequest : AuditableBaseEntityGuid
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
