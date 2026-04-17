using FluentValidation;

namespace Application.Features.ConsentRecords.Commands.RecordConsent;

public class RecordConsentCommandValidator : AbstractValidator<RecordConsentCommand>
{
    public RecordConsentCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(c => c.ConsentText)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(c => c.DocumentType)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(c => c.FormVersion)
            .NotEmpty().WithMessage("{PropertyName} is required.");
    }
}
