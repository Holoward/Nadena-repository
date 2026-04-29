namespace Application.Interfaces;

public class DeliveryResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime DeliveredAt { get; set; }
}

public interface IDataDeliveryService
{
    Task<DeliveryResult> ForwardAsync(
        AnonymizedTakeoutPayload payload,
        string deliveryEndpoint,
        Guid purchaseId);
}
