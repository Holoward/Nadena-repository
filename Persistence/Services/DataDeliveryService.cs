using System.Text;
using System.Text.Json;
using Application.Interfaces;

namespace Persistence.Services;

public class DataDeliveryService : IDataDeliveryService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DataDeliveryService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<DeliveryResult> ForwardAsync(
        AnonymizedTakeoutPayload payload,
        string deliveryEndpoint,
        Guid purchaseId)
    {
        if (string.IsNullOrWhiteSpace(deliveryEndpoint))
            return new DeliveryResult { Success = false, ErrorMessage = "No delivery endpoint configured." };

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
}
