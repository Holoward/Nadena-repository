using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Stores an encrypted Google OAuth refresh token for a contributor. Enables the DrivePollingService to access the contributor's Drive on their behalf without requiring repeated authorization.
/// </summary>
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
    public DateTime? LockedUntil { get; set; }
}
