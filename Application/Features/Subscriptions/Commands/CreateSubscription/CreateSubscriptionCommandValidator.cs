using FluentValidation;

namespace Application.Features.Subscriptions.Commands.CreateSubscription;

public class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(x => x.DatasetId)
            .GreaterThan(0).WithMessage("Dataset ID is required");

        RuleFor(x => x.BuyerId)
            .NotEmpty().WithMessage("Buyer ID is required");

        RuleFor(x => x.PricingModel)
            .Must(x => x == "Monthly" || x == "Quarterly")
            .WithMessage("Pricing model must be Monthly or Quarterly");

        RuleFor(x => x.StripeSubscriptionId)
            .NotEmpty().WithMessage("Stripe subscription ID is required");
    }
}
