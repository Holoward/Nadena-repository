using FluentValidation;

namespace Application.Features.Datasets.Commands.CreateDataset;

public class CreateDatasetCommandValidator : AbstractValidator<CreateDatasetCommand>
{
    public CreateDatasetCommandValidator()
    {
        RuleFor(d => d.Title)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(d => d.Price)
            .GreaterThan(0).WithMessage("{PropertyName} must be greater than 0.");
    }
}
