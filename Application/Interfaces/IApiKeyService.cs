using Domain.Entities;

namespace Application.Interfaces;

public interface IApiKeyService
{
    /// <summary>
    /// Generates a new API key. Returns the raw key (shown once to the buyer),
    /// the SHA-256 hash to persist, and the first-8-char prefix for lookup.
    /// </summary>
    (string RawKey, string KeyHash, string KeyPrefix) GenerateApiKey();

    /// <summary>
    /// Given a raw key from an HTTP header, looks up the ApiKey record by prefix,
    /// verifies the hash, checks expiry and revocation, and returns the entity (or null).
    /// </summary>
    Task<ApiKey?> ValidateAsync(string rawKey);
}
