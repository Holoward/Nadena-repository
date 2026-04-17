using Domain.Common;

namespace Domain.Entities;

public class UserSession : AuditableBaseEntityGuid
{
    public string UserId { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
