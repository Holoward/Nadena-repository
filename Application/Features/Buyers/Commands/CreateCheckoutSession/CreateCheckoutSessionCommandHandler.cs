using Application.Interfaces;
using Application.Wrappers;
using Ardalis.Specification;
using Domain.Entities;
using MediatR;
using Stripe;
using Stripe.Checkout;

using Application.Settings;
using Microsoft.Extensions.Options;

namespace Application.Features.Buyers.Commands.CreateCheckoutSession;

public class CreateCheckoutSessionCommandHandler : IRequestHandler<CreateCheckoutSessionCommand, ServiceResponse<string>>
{
    private readonly IRepositoryAsync<Dataset> _datasetRepository;
    private readonly IRepositoryAsync<Buyer> _buyerRepository;
    private readonly StripeClient _stripeClient;
    private readonly StripeSettings _stripeSettings;

    public CreateCheckoutSessionCommandHandler(
        IRepositoryAsync<Dataset> datasetRepository,
        IRepositoryAsync<Buyer> buyerRepository,
        StripeClient stripeClient,
        IOptions<StripeSettings> stripeSettings)
    {
        _datasetRepository = datasetRepository;
        _buyerRepository = buyerRepository;
        _stripeClient = stripeClient;
        _stripeSettings = stripeSettings.Value;
    }

    public async Task<ServiceResponse<string>> Handle(CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        // Fetch the Dataset by ID to get its Price and Title
        var dataset = await _datasetRepository.GetByIdAsync(request.DatasetId, cancellationToken);
        
        if (dataset == null)
        {
            return new ServiceResponse<string>("Dataset not found");
        }

        // Get buyer info using specification
        var buyerSpec = new BuyerByUserIdSpec(request.BuyerUserId);
        var buyer = await _buyerRepository.FirstOrDefaultAsync(buyerSpec, cancellationToken);

        if (buyer == null)
        {
            return new ServiceResponse<string>("Data Client not found");
        }

        // Get FrontendUrl from StripeSettings
        var frontendUrl = _stripeSettings.FrontendUrl ?? "http://localhost:3000";

        // Create Stripe SessionCreateOptions
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = (long)(dataset.Price * 100), // Convert price to cents
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = dataset.Title,
                            Description = dataset.Description
                        }
                    },
                    Quantity = 1
                }
            },
            SuccessUrl = $"{frontendUrl}/buyer/dashboard?success=true",
            CancelUrl = $"{frontendUrl}/buyer/dashboard?cancelled=true",
            Metadata = new Dictionary<string, string>
            {
                { "datasetId", request.DatasetId.ToString() },
                { "buyerId", buyer.Id.ToString() }
            }
        };

        // Create the Stripe session
        var service = new SessionService(_stripeClient);
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

        // Return the Stripe session URL
        return new ServiceResponse<string>(session.Url, "Checkout session created successfully");
    }
}

// Specification for fetching Buyer by UserId
public class BuyerByUserIdSpec : Specification<Buyer>
{
    public BuyerByUserIdSpec(string userId)
    {
        Query.Where(b => b.UserId == userId);
    }
}
