using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;

namespace Application.Features.Subscriptions.Commands.CreateSubscription;

public class CreateSubscriptionCommand : IRequest<ServiceResponse<string>>
{
    public int DatasetId { get; set; }
    public Guid BuyerId { get; set; }
    public string PricingModel { get; set; } = "Monthly";
    public string StripeSubscriptionId { get; set; } = string.Empty;
}

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, ServiceResponse<string>>
{
    private readonly IDatasetSubscriptionRepository _subscriptionRepository;

    public CreateSubscriptionCommandHandler(IDatasetSubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<ServiceResponse<string>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var nextDelivery = request.PricingModel switch
        {
            "Monthly" => now.AddMonths(1),
            "Quarterly" => now.AddMonths(3),
            _ => now.AddMonths(1)
        };

        var subscription = new DatasetSubscription
        {
            DatasetId = Guid.Empty, // Dataset uses int Id internally; map from request
            BuyerId = request.BuyerId,
            StripeSubscriptionId = request.StripeSubscriptionId,
            PricingModel = request.PricingModel,
            StartDate = now,
            NextDeliveryDate = nextDelivery,
            IsActive = true
        };

        await _subscriptionRepository.AddAsync(subscription);

        return new ServiceResponse<string>($"Subscription created. Next delivery: {nextDelivery:yyyy-MM-dd}");
    }
}
