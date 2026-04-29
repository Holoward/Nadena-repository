using Domain.Common;

namespace Domain.Entities;

public class ContributorOAuthToken : AuditableBaseEntity
{
    public string ContributorId { get; set; } = string.Empty;
    public string EncryptedRefreshToken { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public DateTime? AccessTokenExpiry { get; set; }
    public string GrantedScopes { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastPolledAt { get; set; }
    public bool IsActive { get; set; } = true;
}
