using System.Text;
using System.Text.Json;
using Application.Interfaces;

namespace Persistence.Services;

/// <summary>
/// Forwards anonymized contributor payloads directly to the buyer's configured delivery endpoint. Nothing is stored on Nadena's servers — raw data is deleted immediately after processing.
/// </summary>
public class DataDeliveryService : IDataDeliveryService
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Creates the service with the HTTP client factory used for buyer endpoint delivery.
    /// </summary>
    public DataDeliveryService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Sends an anonymized payload to the buyer's delivery endpoint and reports whether delivery succeeded.
    /// </summary>
    public async Task<DeliveryResult> ForwardAsync(
        AnonymizedTakeoutPayload payload,
        string deliveryEndpoint,
        Guid purchaseId)
    {
        if (string.IsNullOrWhiteSpace(deliveryEndpoint))
            return new DeliveryResult { Success = false, ErrorMessage = "No delivery endpoint configured." };

        if (!IsSafeUrl(deliveryEndpoint))
            return new DeliveryResult { Success = false, ErrorMessage = "Delivery endpoint URL is not safe or allowed." };

        try
        {
            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var request = new HttpRequestMessage(HttpMethod.Post, deliveryEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Nadena-Purchase-Id", purchaseId.ToString());
            request.Headers.Add("X-Nadena-Delivered-At", DateTime.UtcNow.ToString("o"));

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return new DeliveryResult { Success = true, DeliveredAt = DateTime.UtcNow };

            return new DeliveryResult
            {
                Success = false,
                ErrorMessage = $"Endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}"
            };
        }
        catch (HttpRequestException ex)
        {
            return new DeliveryResult { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new DeliveryResult { Success = false, ErrorMessage = $"Unexpected error: {ex.Message}" };
        }
    }

    private bool IsSafeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        if (uri.IsLoopback)
            return false;

        var host = uri.Host.ToLowerInvariant();
        if (host == "localhost" || host == "127.0.0.1" || host == "::1" || 
            host.StartsWith("169.254.") || host.StartsWith("10.") || 
            host.StartsWith("192.168.") || (host.StartsWith("172.") && IsPrivate172(host)))
        {
            return false;
        }

        return true;
    }

    private bool IsPrivate172(string host)
    {
        var parts = host.Split('.');
        if (parts.Length == 4 && int.TryParse(parts[1], out var secondOctet))
        {
            return secondOctet >= 16 && secondOctet <= 31;
        }
        return false;
    }
}
