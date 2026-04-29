using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Persistence.Services;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class OAuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IContributorOAuthTokenRepository _tokenRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IConfiguration configuration,
        IContributorOAuthTokenRepository tokenRepository,
        ICurrentUserService currentUserService,
        IHttpClientFactory httpClientFactory,
        ILogger<OAuthController> logger)
    {
        _configuration = configuration;
        _tokenRepository = tokenRepository;
        _currentUserService = currentUserService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("google-url")]
    [Authorize(Roles = "Data Contributor")]
    public IActionResult GetGoogleOAuthUrl()
    {
        var clientId = _configuration["NadenaSettings:GoogleClientId"];
        var redirectUri = _configuration["NadenaSettings:GoogleRedirectUri"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
            return StatusCode(500, new { error = "Google OAuth is not configured." });

        var state = _currentUserService.GetCurrentUserId();
        var scope = Uri.EscapeDataString("https://www.googleapis.com/auth/drive.readonly");
        var url = $"https://accounts.google.com/o/oauth2/v2/auth" +
                  $"?client_id={Uri.EscapeDataString(clientId)}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&response_type=code" +
                  $"&scope={scope}" +
                  $"&access_type=offline" +
                  $"&prompt=consent" +
                  $"&state={Uri.EscapeDataString(state)}";

        return Ok(new { url });
    }

    [HttpGet("callback")]
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

        var contributorId = state;
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
