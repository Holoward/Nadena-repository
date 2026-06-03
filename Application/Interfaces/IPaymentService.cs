using Application.Wrappers;

namespace Application.Interfaces;

public interface IPaymentService
{
    Task<ServiceResponse<string>> ProcessPurchaseAsync(Guid dataClientUserId, Guid datasetId, decimal price, string billingType, string idempotencyKey, bool contributorShareNow);
    Task<ServiceResponse<string>> ReleaseHeldPayoutAsync(Guid transactionId, string approvedByUserId);
    Task<ServiceResponse<string>> MarkDisbursedExternallyAsync(Guid transactionId, string adminUserId, string? notes = null);
}
