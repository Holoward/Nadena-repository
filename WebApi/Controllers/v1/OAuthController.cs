using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Persistence.Services;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
/// <summary>
/// Manages Google OAuth authorization for contributors. Generates the authorization URL and handles the callback to store encrypted refresh tokens.
/// </summary>
public class OAuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IContributorOAuthTokenRepository _tokenRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OAuthController> _logger;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Creates the controller with configuration, token storage, current-user, HTTP, and logging services.
    /// </summary>
    public OAuthController(
        IConfiguration configuration,
        IContributorOAuthTokenRepository tokenRepository,
        ICurrentUserService currentUserService,
        IHttpClientFactory httpClientFactory,
        ILogger<OAuthController> logger,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _tokenRepository = tokenRepository;
        _currentUserService = currentUserService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cache = cache;
    }

    [HttpGet("google-url")]
    [Authorize(Roles = "Data Contributor")]
    /// <summary>
    /// Builds the Google OAuth authorization URL for the signed-in contributor.
    /// </summary>
    public IActionResult GetGoogleOAuthUrl()
    {
        var clientId = _configuration["NadenaSettings:GoogleClientId"];
        var redirectUri = _configuration["NadenaSettings:GoogleRedirectUri"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
            return StatusCode(500, new { error = "Google OAuth is not configured." });

        // Generate a cryptographic nonce and bind it to the current user in cache
        // The nonce is unpredictable, short-lived (10 min), and validated on callback
        var userId = _currentUserService.GetCurrentUserId();
        var nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        _cache.Set($"oauth_nonce:{nonce}", userId, TimeSpan.FromMinutes(10));

        var scope = Uri.EscapeDataString("https://www.googleapis.com/auth/drive.readonly");
        var url = $"https://accounts.google.com/o/oauth2/v2/auth" +
                  $"?client_id={Uri.EscapeDataString(clientId)}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&response_type=code" +
                  $"&scope={scope}" +
                  $"&access_type=offline" +
                  $"&prompt=consent" +
                  $"&state={Uri.EscapeDataString(nonce)}";

        return Ok(new { url });
    }

    [HttpGet("callback")]
    /// <summary>
    /// Handles Google's OAuth callback, exchanges the code for tokens, and stores the contributor's encrypted refresh token.
    /// </summary>
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        [FromQuery] string? error = null)
    {
        var frontendUrl = _configuration["NadenaSettings:FrontendUrl"] ?? "http://localhost:3000";

        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("Google OAuth denied: {Error}", error);
            return Redirect($"{frontendUrl}/upload?oauth=denied");
        }

        var clientId = _configuration["NadenaSettings:GoogleClientId"];
        var clientSecret = _configuration["NadenaSettings:GoogleClientSecret"];
        var redirectUri = _configuration["NadenaSettings:GoogleRedirectUri"];
        var encryptionKey = _configuration["NadenaSettings:TokenEncryptionKey"];

        var client = _httpClientFactory.CreateClient();
        var tokenResponse = await client.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", clientId!),
                new KeyValuePair<string, string>("client_secret", clientSecret!),
                new KeyValuePair<string, string>("redirect_uri", redirectUri!),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            }));

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Token exchange failed: {Json}", tokenJson);
            return Redirect($"{frontendUrl}/upload?oauth=error");
        }

        using var doc = System.Text.Json.JsonDocument.Parse(tokenJson);
        var accessToken = doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
        var refreshToken = doc.RootElement.TryGetProperty("refresh_token", out var rt)
            ? rt.GetString() ?? string.Empty
            : string.Empty;

        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("No refresh token returned for contributor {State}.", state);
            return Redirect($"{frontendUrl}/upload?oauth=no_refresh_token");
        }

        // Validate the nonce — reject the callback if it wasn't issued by us
        if (!_cache.TryGetValue($"oauth_nonce:{state}", out string? contributorId)
            || string.IsNullOrEmpty(contributorId))
        {
            _logger.LogWarning("OAuth callback received invalid or expired state nonce.");
            return Redirect($"{frontendUrl}/upload?oauth=error");
        }
        // Consume the nonce so it cannot be replayed
        _cache.Remove($"oauth_nonce:{state}");

        var encryptedRefresh = string.IsNullOrEmpty(encryptionKey)
            ? refreshToken
            : GoogleDriveService.Encrypt(refreshToken, encryptionKey);

        var existing = await _tokenRepository.GetByContributorIdAsync(contributorId);
        if (existing != null)
        {
            existing.EncryptedRefreshToken = encryptedRefresh;
            existing.AccessToken = accessToken;
            existing.AccessTokenExpiry = DateTime.UtcNow.AddHours(1);
            existing.GrantedAt = DateTime.UtcNow;
            existing.IsActive = true;
            await _tokenRepository.UpdateAsync(existing);
        }
        else
        {
            await _tokenRepository.AddAsync(new ContributorOAuthToken
            {
                ContributorId = contributorId,
                EncryptedRefreshToken = encryptedRefresh,
                AccessToken = accessToken,
                AccessTokenExpiry = DateTime.UtcNow.AddHours(1),
                GrantedScopes = "drive.readonly",
                GrantedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedBy = "System",
                Created = DateTime.UtcNow
            });
        }

        _logger.LogInformation("OAuth token stored for contributor {ContributorId}.", contributorId);
        return Redirect($"{frontendUrl}/upload?oauth=success");
    }
}
