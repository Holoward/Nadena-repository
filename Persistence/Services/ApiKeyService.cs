using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using System.Security.Cryptography;
using System.Text;

namespace Persistence.Services;

/// <summary>
/// API key service using the PAT pattern:
/// - Generates cryptographically random keys
/// - Stores only the SHA-256 hash
/// - Validates by hashing the incoming raw key and comparing
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly ApplicationDbContext _dbContext;

    public ApiKeyService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public (string RawKey, string KeyHash, string KeyPrefix) GenerateApiKey()
    {
        // 32 random bytes → 64 char hex string (256-bit entropy)
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawKey = "ndna_" + Convert.ToHexString(rawBytes).ToLowerInvariant(); // e.g. ndna_8af3c2...
        var prefix = rawKey[..8]; // first 8 chars for fast lookup
        var hash = ComputeHash(rawKey);
        return (rawKey, hash, prefix);
    }

    public async Task<ApiKey?> ValidateAsync(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey) || rawKey.Length < 8)
            return null;

        var prefix = rawKey[..8];
        var hash = ComputeHash(rawKey);

        // Find by prefix first (fast), then verify hash
        var apiKey = await _dbContext.ApiKeys
            .FirstOrDefaultAsync(k =>
                k.KeyPrefix == prefix &&
                k.KeyHash == hash &&
                !k.IsRevoked &&
                k.ExpiresAt > DateTime.UtcNow);

        if (apiKey != null)
        {
            apiKey.LastUsedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        return apiKey;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
