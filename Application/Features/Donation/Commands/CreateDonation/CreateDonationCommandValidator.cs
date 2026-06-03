using FluentValidation;

namespace Application.Features.Donation.Commands.CreateDonation;

public class CreateDonationCommandValidator : AbstractValidator<CreateDonationCommand>
{
    public CreateDonationCommandValidator()
    {
        RuleFor(x => x.ContributorId)
            .NotEmpty()
            .WithMessage("Contributor ID is required");

        RuleFor(x => x.ConsentVersion)
            .NotEmpty()
            .WithMessage("Consent version is required")
            .MaximumLength(50)
            .WithMessage("Consent version must not exceed 50 characters");
    }
}