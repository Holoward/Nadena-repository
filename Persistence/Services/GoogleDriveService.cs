using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Persistence.Services;

public class GoogleDriveService : IGoogleDriveService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public GoogleDriveService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string> RefreshAccessTokenAsync(string encryptedRefreshToken)
    {
        var encryptionKey = _configuration["NadenaSettings:TokenEncryptionKey"];
        var refreshToken = string.IsNullOrEmpty(encryptionKey)
            ? encryptedRefreshToken
            : Decrypt(encryptedRefreshToken, encryptionKey);

        var clientId = _configuration["NadenaSettings:GoogleClientId"];
        var clientSecret = _configuration["NadenaSettings:GoogleClientSecret"];

        var client = _httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id", clientId!),
            new KeyValuePair<string, string>("client_secret", clientSecret!)
        });

        var response = await client.PostAsync("https://oauth2.googleapis.com/token", content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Token refresh failed: {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new Exception("No access_token in refresh response");
    }

    public async Task<List<DriveFile>> FindTakeoutFilesAsync(string accessToken, DateTime newerThan)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var newerThanRfc = newerThan.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var query = Uri.EscapeDataString(
            $"name contains 'takeout' and mimeType = 'application/zip' and createdTime > '{newerThanRfc}'");
        var url = $"https://www.googleapis.com/drive/v3/files?q={query}&fields=files(id,name,createdTime,size)&orderBy=createdTime desc";

        var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return new List<DriveFile>();

        using var doc = JsonDocument.Parse(json);
        var files = new List<DriveFile>();

        if (doc.RootElement.TryGetProperty("files", out var filesArr))
        {
            foreach (var f in filesArr.EnumerateArray())
            {
                files.Add(new DriveFile
                {
                    Id = f.GetProperty("id").GetString() ?? string.Empty,
                    Name = f.GetProperty("name").GetString() ?? string.Empty,
                    CreatedTime = f.TryGetProperty("createdTime", out var ct)
                        ? DateTime.Parse(ct.GetString()!)
                        : DateTime.UtcNow,
                    Size = f.TryGetProperty("size", out var sz)
                        ? long.TryParse(sz.GetString(), out var szVal) ? szVal : 0
                        : 0
                });
            }
        }

        return files;
    }

    public async Task<Stream> DownloadFileAsync(string accessToken, string fileId)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(
            $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media");

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Drive download failed: {response.StatusCode}");

        var ms = new MemoryStream();
        await response.Content.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }

    public static string Encrypt(string plainText, string base64Key)
    {
        var keyBytes = Convert.FromBase64String(base64Key);
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = keyBytes;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var result = new byte[aes.IV.Length + encrypted.Length];
        aes.IV.CopyTo(result, 0);
        encrypted.CopyTo(result, aes.IV.Length);
        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherText, string base64Key)
    {
        var fullBytes = Convert.FromBase64String(cipherText);
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.IV = fullBytes[..16];
        aes.Key = Convert.FromBase64String(base64Key);
        using var decryptor = aes.CreateDecryptor();
        var cipher = fullBytes[16..];
        var decrypted = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(decrypted);
    }
}
