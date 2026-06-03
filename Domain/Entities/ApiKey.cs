using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// API key for B2B data access. Raw key is returned once at creation;
/// only the SHA-256 hash is persisted (GitHub PAT pattern).
/// </summary>
public class ApiKey : AuditableBaseEntityGuid
{
    public string BuyerId { get; set; }

    /// <summary>First 8 characters of the raw key — used for fast lookup before hash comparison</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the full raw key</summary>
    public string KeyHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime? LastUsedAt { get; set; }
}
