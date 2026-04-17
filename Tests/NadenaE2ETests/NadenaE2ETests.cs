using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace NadenaE2ETests;

/// <summary>
/// End-to-end tests for the Nadena backend API.
/// These tests require the API to be running locally on http://localhost:5034
/// 
/// To run these tests:
/// 1. Start the API: dotnet run --project WebApi
/// 2. Run tests: dotnet test Tests/NadenaE2ETests
/// </summary>
public class NadenaE2ETests : IDisposable
{
    private readonly HttpClient _client;
    private readonly string _baseUrl = "http://localhost:5034";

    // Test data
    private string _volunteerEmail = $"volunteer_{Guid.NewGuid():N}@test.com";
    private string _volunteerPassword = "TestPassword123!";
    private string _volunteerFullName = "Test Volunteer";
    private string _volunteerPayPalEmail = "volunteer@paypal.com";
    private string? _volunteerToken;
    private string? _volunteerUserId;
    private int? _volunteerId;
    private bool _fileUploaded = false;

    private string _adminEmail = "admin@nadena.com";
    private string _adminPassword = "AdminPassword123!";
    private string? _adminToken;

    private string _buyerEmail = $"buyer_{Guid.NewGuid():N}@test.com";
    private string _buyerPassword = "BuyerPassword123!";
    private string? _buyerToken;
    private int? _datasetId;

    public NadenaE2ETests()
    {
        _client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    #region Test 1: Register Volunteer

    [Fact]
    public async Task RegisterVolunteer_ShouldReturnSuccess()
    {
        var registerRequest = new
        {
            fullName = _volunteerFullName,
            email = _volunteerEmail,
            password = _volunteerPassword,
            role = "Volunteer",
            payPalEmail = _volunteerPayPalEmail
        };

        var response = await _client.PostAsJsonAsync("/api/v1/Auth/register", registerRequest);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created,
            $"Expected OK or Created, got {response.StatusCode}");
    }

    #endregion

    #region Test 2: Login as Volunteer and Get JWT Token

    [Fact]
    public async Task LoginVolunteer_ShouldReturnJwtToken()
    {
        // First register the volunteer if not already done
        await RegisterVolunteer_ShouldReturnSuccess();

        var loginRequest = new
        {
            email = _volunteerEmail,
            password = _volunteerPassword
        };

        var response = await _client.PostAsJsonAsync("/api/v1/Auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Parse the response to get token
        using var doc = System.Text.Json.JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("data", out var data))
        {
            if (data.ValueKind == System.Text.Json.JsonValueKind.String)
                _volunteerToken = data.GetString();
            else if (data.TryGetProperty("token", out var tokenElement))
                _volunteerToken = tokenElement.GetString();
        }
        else if (root.TryGetProperty("token", out var tokenElement2))
        {
            _volunteerToken = tokenElement2.GetString();
        }

        if (!string.IsNullOrEmpty(_volunteerToken))
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(_volunteerToken);
            _volunteerUserId = jwtToken.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value;
        }

        Assert.False(string.IsNullOrEmpty(_volunteerToken), "JWT token should not be null or empty");
    }

    #endregion

    #region Test 3: Get Volunteer ID

    [Fact]
    public async Task GetVolunteerId_ShouldReturnVolunteerId()
    {
        // Ensure volunteer is logged in
        if (string.IsNullOrEmpty(_volunteerToken))
        {
            await LoginVolunteer_ShouldReturnJwtToken();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _volunteerToken);

        var response = await _client.GetAsync($"/api/v1/Volunteer/user/{_volunteerUserId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Parse volunteer ID from response
        using var doc = System.Text.Json.JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("id", out var idElement))
            {
                _volunteerId = idElement.GetInt32();
            }
        }
        else if (root.TryGetProperty("id", out var idElement2))
        {
            _volunteerId = idElement2.GetInt32();
        }

        Assert.True(_volunteerId.HasValue, "Volunteer ID should be retrieved");
    }

    #endregion

    #region Test 4: Upload Google Takeout ZIP File

    [Fact]
    public async Task UploadFile_ShouldReturnCommentCount()
    {
        // Ensure volunteer is logged in
        if (string.IsNullOrEmpty(_volunteerToken))
        {
            await LoginVolunteer_ShouldReturnJwtToken();
        }

        if (!_volunteerId.HasValue)
        {
            await GetVolunteerId_ShouldReturnVolunteerId();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _volunteerToken);

        // Create a minimal ZIP file for testing
        var testZipPath = Path.Combine(Path.GetTempPath(), $"test_takeout_{Guid.NewGuid():N}.zip");

        // Create a minimal ZIP file with sample YouTube comment data
        using (var zipStream = new FileStream(testZipPath, FileMode.Create))
        using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("youtube-takeout/comments.html");
            using var writer = new StreamWriter(entry.Open());
            writer.Write(@"<!DOCTYPE html>
<html>
<head><title>YouTube Comment History</title></head>
<body>
<div class=""comment"">
  <div class=""author"">Test User</div>
  <div class=""text"">This is a test comment</div>
  <div class=""time"">2024-01-15T10:30:00Z</div>
</div>
</body>
</html>");
        }

        try
        {
            using var fileStream = File.OpenRead(testZipPath);
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(fileContent, "file", "youtube-takeout.zip");

            var response = await _client.PostAsync("/api/v1/Volunteer/upload-file", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();

            // Verify comment count is returned in response
            using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            bool hasCommentCount = false;
            if (root.TryGetProperty("data", out var data))
            {
                hasCommentCount = data.TryGetProperty("commentCount", out _) ||
                                  data.TryGetProperty("commentsProcessed", out _);
            }
            else
            {
                hasCommentCount = root.TryGetProperty("commentCount", out _) ||
                                 root.TryGetProperty("commentsProcessed", out _);
            }

            Assert.True(hasCommentCount, "Response should contain comment count");
            _fileUploaded = true;
        }
        finally
        {
            // Cleanup test file
            if (File.Exists(testZipPath))
            {
                File.Delete(testZipPath);
            }
        }
    }

    #endregion

    #region Test 5: Register Admin User (Prerequisite)

    [Fact]
    public async Task RegisterAdmin_ShouldReturnSuccess()
    {
        var registerRequest = new
        {
            fullName = "Admin User",
            email = _adminEmail,
            password = _adminPassword,
            role = "Admin",
            payPalEmail = "admin@paypal.com"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/Auth/register", registerRequest);

        // Allow for already exists scenario
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Created ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected OK/Created/BadRequest, got {response.StatusCode}");
    }

    #endregion

    #region Test 6: Login as Admin

    [Fact]
    public async Task LoginAdmin_ShouldReturnJwtToken()
    {
        // Ensure admin is registered
        await RegisterAdmin_ShouldReturnSuccess();

        var loginRequest = new
        {
            email = _adminEmail,
            password = _adminPassword
        };

        var response = await _client.PostAsJsonAsync("/api/v1/Auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Parse the response to get token
        using var doc = System.Text.Json.JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("data", out var data))
        {
            if (data.ValueKind == System.Text.Json.JsonValueKind.String)
                _adminToken = data.GetString();
            else if (data.TryGetProperty("token", out var tokenElement))
                _adminToken = tokenElement.GetString();
        }
        else if (root.TryGetProperty("token", out var tokenElement2))
        {
            _adminToken = tokenElement2.GetString();
        }

        Assert.False(string.IsNullOrEmpty(_adminToken), "Admin JWT token should not be null or empty");
    }

    #endregion

    #region Test 7: Build Dataset (Admin)

    [Fact]
    public async Task BuildDataset_ShouldGenerateCsvFile()
    {
        // Ensure admin is logged in
        if (string.IsNullOrEmpty(_adminToken))
        {
            await LoginAdmin_ShouldReturnJwtToken();
        }

        // Ensure volunteer user ID is available
        if (string.IsNullOrEmpty(_volunteerUserId))
        {
            await LoginVolunteer_ShouldReturnJwtToken();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        // Build dataset using volunteer's user ID
        var buildRequest = new
        {
            volunteerIds = new List<string> { _volunteerUserId },
            datasetName = $"Test Dataset {DateTime.UtcNow:yyyyMMddHHmmss}",
            price = 99.99m
        };

        var response = await _client.PostAsJsonAsync("/api/v1/Dataset/build", buildRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Parse the response to get dataset ID and download URL
        using var doc = System.Text.Json.JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("datasetId", out var datasetIdElement))
            {
                _datasetId = datasetIdElement.GetInt32();
            }
            if (data.TryGetProperty("downloadUrl", out var downloadUrlElement))
            {
                var downloadUrl = downloadUrlElement.GetString();
                Assert.False(string.IsNullOrEmpty(downloadUrl), "Download URL should be returned");
            }
        }

        Assert.True(_datasetId.HasValue, "Dataset ID should be returned");
    }

    #endregion

    #region Test 8: Register Buyer User

    [Fact]
    public async Task RegisterBuyer_ShouldReturnSuccess()
    {
        var registerRequest = new
        {
            fullName = "Test Buyer",
            email = _buyerEmail,
            password = _buyerPassword,
            role = "Buyer",
            payPalEmail = ""
        };

        var response = await _client.PostAsJsonAsync("/api/v1/Auth/register", registerRequest);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Created,
            $"Expected OK or Created, got {response.StatusCode}");
    }

    #endregion

    #region Test 9: Login as Buyer

    [Fact]
    public async Task LoginBuyer_ShouldReturnJwtToken()
    {
        // Ensure buyer is registered
        await RegisterBuyer_ShouldReturnSuccess();

        var loginRequest = new
        {
            email = _buyerEmail,
            password = _buyerPassword
        };

        var response = await _client.PostAsJsonAsync("/api/v1/Auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Parse the response to get token
        using var doc = System.Text.Json.JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("data", out var data))
        {
            if (data.ValueKind == System.Text.Json.JsonValueKind.String)
                _buyerToken = data.GetString();
            else if (data.TryGetProperty("token", out var tokenElement))
                _buyerToken = tokenElement.GetString();
        }
        else if (root.TryGetProperty("token", out var tokenElement2))
        {
            _buyerToken = tokenElement2.GetString();
        }

        Assert.False(string.IsNullOrEmpty(_buyerToken), "Buyer JWT token should not be null or empty");
    }

    #endregion

    #region Test 10: Buyer Checkout (Stripe Session)

    [Fact]
    public async Task BuyerCheckout_ShouldReturnStripeSessionUrl()
    {
        // Ensure buyer is logged in
        if (string.IsNullOrEmpty(_buyerToken))
        {
            await LoginBuyer_ShouldReturnJwtToken();
        }

        // Ensure dataset is built
        if (!_datasetId.HasValue)
        {
            await BuildDataset_ShouldGenerateCsvFile();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _buyerToken);

        var checkoutRequest = new
        {
            datasetId = _datasetId.Value
        };

        var response = await _client.PostAsJsonAsync("/api/v1/Buyer/checkout", checkoutRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify Stripe session URL is returned
        using var doc = System.Text.Json.JsonDocument.Parse(content);
        var root = doc.RootElement;

        bool hasUrl = false;
        string? stripeUrl = null;

        if (root.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("url", out var urlElement))
            {
                stripeUrl = urlElement.GetString();
                hasUrl = !string.IsNullOrEmpty(stripeUrl) && stripeUrl.Contains("stripe");
            }
        }
        else if (root.TryGetProperty("url", out var urlElement2))
        {
            stripeUrl = urlElement2.GetString();
            hasUrl = !string.IsNullOrEmpty(stripeUrl) && stripeUrl.Contains("stripe");
        }

        Assert.True(hasUrl, "Response should contain Stripe session URL");
    }

    #endregion

    #region Test 11: Pay Volunteers (Admin PayPal)

    [Fact]
    public async Task PayVolunteers_ShouldReturnPayPalBatchId()
    {
        // Ensure admin is logged in
        if (string.IsNullOrEmpty(_adminToken))
        {
            await LoginAdmin_ShouldReturnJwtToken();
        }

        // Ensure volunteer user ID is available
        if (string.IsNullOrEmpty(_volunteerUserId))
        {
            await LoginVolunteer_ShouldReturnJwtToken();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var payRequest = new
        {
            volunteerIds = new List<string> { _volunteerUserId },
            datasetId = _datasetId ?? 1,
            totalRevenue = 50.00m
        };

        var response = await _client.PostAsJsonAsync("/api/v1/Admin/pay-volunteers", payRequest);

        // May return OK or BadRequest depending on PayPal configuration
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected OK or BadRequest, got {response.StatusCode}");

        // If successful, verify PayPal batch ID is returned
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            var root = doc.RootElement;

            bool hasBatchId = false;
            if (root.TryGetProperty("data", out var data))
            {
                hasBatchId = data.TryGetProperty("batchId", out _) ||
                            data.TryGetProperty("payPalBatchId", out _);
            }
            else
            {
                hasBatchId = root.TryGetProperty("batchId", out _) ||
                            root.TryGetProperty("payPalBatchId", out _);
            }

            Assert.True(hasBatchId, "Response should contain PayPal batch ID");
        }
    }

    #endregion

    #region Full Integration Test

    /// <summary>
    /// Runs the complete end-to-end workflow in sequence.
    /// </summary>
    [Fact]
    public async Task CompleteEndToEndWorkflow_ShouldSucceed()
    {
        // Step 1: Register volunteer
        await RegisterVolunteer_ShouldReturnSuccess();

        // Step 2: Login as volunteer
        await LoginVolunteer_ShouldReturnJwtToken();

        // Step 3: Get volunteer ID
        await GetVolunteerId_ShouldReturnVolunteerId();

        // Step 4: Upload file
        await UploadFile_ShouldReturnCommentCount();

        // Step 5: Register admin
        await RegisterAdmin_ShouldReturnSuccess();

        // Step 6: Login as admin
        await LoginAdmin_ShouldReturnJwtToken();

        // Step 7: Build dataset
        await BuildDataset_ShouldGenerateCsvFile();

        // Step 8: Register buyer
        await RegisterBuyer_ShouldReturnSuccess();

        // Step 9: Login as buyer
        await LoginBuyer_ShouldReturnJwtToken();

        // Step 10: Buyer checkout
        await BuyerCheckout_ShouldReturnStripeSessionUrl();

        // Step 11: Pay volunteers
        await PayVolunteers_ShouldReturnPayPalBatchId();
    }

    #endregion
}
