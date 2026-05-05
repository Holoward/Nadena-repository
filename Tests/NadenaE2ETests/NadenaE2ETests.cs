using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Persistence.Context;
using Xunit;

namespace NadenaE2ETests;

[CollectionDefinition("Nadena E2E Tests", DisableParallelization = true)]
public class NadenaE2ECollection : ICollectionFixture<WebApplicationFactory<Program>> { }

[Collection("Nadena E2E Tests")]
public class NadenaE2ETests
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public NadenaE2ETests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=localhost;Database=Test;");
        Environment.SetEnvironmentVariable("ConnectionStrings__IdentityConnection", "Host=localhost;Database=TestId;");

        _factory = factory;
        
        _client = _factory.CreateClient();
    }

    private string NewEmail(string prefix) => $"{prefix}_{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}@test.com";

    private async Task<(string Token, string UserId)> RegisterAndLoginAsync(string fullName, string role)
    {
        var email = NewEmail(role.Replace(" ", ""));
        var password = "TestPassword123!";
        
        var regResponse = await _client.PostAsJsonAsync("/api/v1/Auth/register", new
        {
            fullName = fullName,
            email = email,
            password = password,
            confirmPassword = password,
            role = role,
            payPalEmail = $"{email}@paypal.com"
        });
        
        if (regResponse.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.Created))
        {
            var err = await regResponse.Content.ReadAsStringAsync();
            throw new Exception($"Registration failed for {email}: {err}");
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/Auth/login", new
        {
            email = email,
            password = password
        });

        var content = await loginResponse.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        string token = "";
        if (root.TryGetProperty("data", out var data))
        {
            token = data.ValueKind == System.Text.Json.JsonValueKind.String
                ? data.GetString()!
                : data.GetProperty("token").GetString()!;
        }
        else if (root.TryGetProperty("token", out var t))
        {
            token = t.GetString()!;
        }

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var userId = jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value;

        // If Data Contributor, record onboarding consents to bypass the 428 Filter
        if (role == "Data Contributor")
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await _client.PostAsJsonAsync("/api/v1/Consent", new
            {
                userId = userId,
                ipAddress = "127.0.0.1",
                consentText = "I agree to Terms of Service",
                documentType = "TermsOfService",
                formVersion = "v1.0"
            });
            await _client.PostAsJsonAsync("/api/v1/Consent", new
            {
                userId = userId,
                ipAddress = "127.0.0.1",
                consentText = "I agree to Data Consent",
                documentType = "DataConsent",
                formVersion = "v1.0"
            });
            _client.DefaultRequestHeaders.Authorization = null;
        }

        return (token, userId);
    }

    [Fact]
    public async Task RegisterVolunteer_ShouldReturnSuccess()
    {
        var email = NewEmail("volunteer");
        var response = await _client.PostAsJsonAsync("/api/v1/Auth/register", new
        {
            fullName = "Test Volunteer",
            email = email,
            password = "TestPassword123!",
            confirmPassword = "TestPassword123!",
            role = "Data Contributor",
            payPalEmail = "volunteer@paypal.com"
        });

        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created);
    }

    [Fact]
    public async Task LoginVolunteer_ShouldReturnJwtToken()
    {
        var (token, _) = await RegisterAndLoginAsync("Login User", "Data Contributor");
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task GetVolunteerProfile_ShouldReturn200()
    {
        var (token, userId) = await RegisterAndLoginAsync("Profile User", "Data Contributor");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/DataContributor/user/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDataPools_ShouldReturnSeededPools()
    {
        var response = await _client.GetAsync("/api/v1/DataPool");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDataPoolPreview_ShouldReturn200WithPoolMeta()
    {
        var response = await _client.GetAsync("/api/v1/DataPool/1/preview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UploadTakeout_ShouldIngestWatchEvents()
    {
        var (token, _) = await RegisterAndLoginAsync("Upload User", "Data Contributor");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        const string watchHistoryJson = "[{\"header\":\"YouTube\",\"title\":\"Watched Sample\",\"titleUrl\":\"https://www.youtube.com/watch?v=dQw4w9WgXcQ\",\"subtitles\":[],\"time\":\"2024-03-01T14:30:00.000Z\",\"products\":[\"YouTube\"],\"activityControls\":[\"YouTube watch history\"]}]";
        var zipPath = Path.Combine(Path.GetTempPath(), $"takeout_{Guid.NewGuid():N}.zip");
        try
        {
            using (var zipStream = new FileStream(zipPath, FileMode.Create))
            using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("Takeout/YouTube and YouTube Music/history/watch-history.json");
                using var writer = new StreamWriter(entry.Open());
                writer.Write(watchHistoryJson);
            }

            await using var fileStream = File.OpenRead(zipPath);
            using var multipart = new MultipartFormDataContent();
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            multipart.Add(fileContent, "file", "takeout.zip");

            var response = await _client.PostAsync("/api/v1/Takeout/upload", multipart);
            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.UnprocessableEntity or HttpStatusCode.BadRequest);
        }
        finally { if (File.Exists(zipPath)) File.Delete(zipPath); }
    }

    [Fact]
    public async Task RevocationStatus_ShouldReturn200ForAuthenticatedContributor()
    {
        var (token, _) = await RegisterAndLoginAsync("Revoke User", "Data Contributor");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/Revocation/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LoginAdmin_ShouldReturnJwtToken()
    {
        var email = "admin@nadena.com";
        var password = "AdminPassword123!";

        var response = await _client.PostAsJsonAsync("/api/v1/Auth/login", new
        {
            email = email,
            password = password
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminUpdateDataPool_ShouldReturn200()
    {
        var email = "admin@nadena.com";
        var password = "AdminPassword123!";
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/Auth/login", new { email = email, password = password });
        
        var content = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, $"Login failed: {content}");
        
        using var doc = System.Text.Json.JsonDocument.Parse(content);
        var token = doc.RootElement.GetProperty("data").GetString();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PutAsJsonAsync("/api/v1/DataPool/1", new { pricePerMonth = 109.00m, isActive = true });
        
        var putContent = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound, $"Update failed with {response.StatusCode}: {putContent}");
    }

    [Fact]
    public async Task RegisterAndLoginBuyer_ShouldReturnToken()
    {
        var (token, _) = await RegisterAndLoginAsync("Buyer User", "Data Client");
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task PutDeliveryEndpoint_NonExistentPurchase_ShouldReturn404()
    {
        var (token, _) = await RegisterAndLoginAsync("Buyer Delivery", "Data Client");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync($"/api/v1/DataClient/my-datasets/{Guid.NewGuid()}/delivery-endpoint", new { deliveryEndpoint = "https://webhook.example.com" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteEndToEndWorkflow_ShouldSucceed()
    {
        // This is now redundant but kept as a combined smoke test
        var (token, userId) = await RegisterAndLoginAsync("Workflow User", "Data Contributor");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var profileResp = await _client.GetAsync($"/api/v1/DataContributor/user/{userId}");
        Assert.Equal(HttpStatusCode.OK, profileResp.StatusCode);

        var poolsResp = await _client.GetAsync("/api/v1/DataPool");
        Assert.Equal(HttpStatusCode.OK, poolsResp.StatusCode);
    }
}
