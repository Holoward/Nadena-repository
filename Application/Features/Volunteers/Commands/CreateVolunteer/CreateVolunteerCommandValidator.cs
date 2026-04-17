using FluentValidation;

namespace Application.Features.Volunteers.Commands.CreateVolunteer;

public class CreateVolunteerCommandValidator : AbstractValidator<CreateVolunteerCommand>
{
    public CreateVolunteerCommandValidator()
    {
        RuleFor(v => v.UserId)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(v => v.YouTubeAccountAge)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters.");

        RuleFor(v => v.CommentCountEstimate)
            .GreaterThan(0).WithMessage("{PropertyName} must be greater than 0.");

        RuleFor(v => v.ContentTypes)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");
    }
}
