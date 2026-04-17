using FluentValidation;

namespace Application.Features.Buyers.Commands.CreateBuyer;

public class CreateBuyerCommandValidator : AbstractValidator<CreateBuyerCommand>
{
    public CreateBuyerCommandValidator()
    {
        RuleFor(b => b.UserId)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(b => b.CompanyName)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(200).WithMessage("{PropertyName} must not exceed 200 characters.");

        RuleFor(b => b.UseCase)
            .MaximumLength(1000).WithMessage("{PropertyName} must not exceed 1000 characters.");

        RuleFor(b => b.Website)
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.")
            .Must(BeValidUrl).WithMessage("{PropertyName} must be a valid URL.");
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true; // Allow empty, but not invalid

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
