using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Application.Features.Admin.Commands.ProcessRefund;

public class ProcessRefundCommand : IRequest<ServiceResponse<string>>
{
    public Guid PurchaseId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ProcessRefundCommandHandler : IRequestHandler<ProcessRefundCommand, ServiceResponse<string>>
{
    private readonly IRepositoryAsync<DatasetPurchase> _purchaseRepository;
    private readonly StripeClient _stripeClient;
    private readonly ILogger<ProcessRefundCommandHandler> _logger;

    public ProcessRefundCommandHandler(
        IRepositoryAsync<DatasetPurchase> purchaseRepository,
        StripeClient stripeClient,
        ILogger<ProcessRefundCommandHandler> logger)
    {
        _purchaseRepository = purchaseRepository;
        _stripeClient = stripeClient;
        _logger = logger;
    }

    public async Task<ServiceResponse<string>> Handle(ProcessRefundCommand request, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(request.PurchaseId);
        if (purchase == null)
        {
            return new ServiceResponse<string>("Purchase not found");
        }

        if (purchase.IsRefunded)
        {
            return new ServiceResponse<string>("Purchase has already been refunded");
        }

        try
        {
            // Process Stripe refund
            var refundService = new RefundService(_stripeClient);
            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = purchase.StripeSessionId,
                Reason = "requested_by_customer"
            };

            await refundService.CreateAsync(refundOptions);

            // Mark purchase as refunded
            purchase.IsRefunded = true;
            await _purchaseRepository.UpdateAsync(purchase);

            _logger.LogInformation("Refund processed for PurchaseId={PurchaseId}", request.PurchaseId);

            return new ServiceResponse<string>("Refund processed successfully");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund failed for PurchaseId={PurchaseId}", request.PurchaseId);
            return new ServiceResponse<string>($"Refund failed: {ex.Message}");
        }
    }
}
