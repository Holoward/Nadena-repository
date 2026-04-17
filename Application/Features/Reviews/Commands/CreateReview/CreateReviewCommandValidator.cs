using FluentValidation;

namespace Application.Features.Reviews.Commands.CreateReview;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.DatasetId)
            .GreaterThan(0).WithMessage("Dataset ID is required");

        RuleFor(x => x.BuyerId)
            .NotEmpty().WithMessage("Buyer ID is required");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5");

        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters");
    }
}
