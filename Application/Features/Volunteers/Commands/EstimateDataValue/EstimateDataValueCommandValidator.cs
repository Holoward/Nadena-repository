using FluentValidation;

namespace Application.Features.Volunteers.Commands.EstimateDataValue;

public class EstimateDataValueCommandValidator : AbstractValidator<EstimateDataValueCommand>
{
    public EstimateDataValueCommandValidator()
    {
        RuleFor(v => v.CommentCountEstimate)
            .GreaterThan(0).WithMessage("{PropertyName} must be greater than 0.");

        RuleFor(v => v.ContentTypes)
            .NotEmpty().WithMessage("{PropertyName} is required.");
    }
}
