using FluentValidation;

namespace Application.Features.Volunteers.Commands.RequestDataDeletion;

public class RequestDataDeletionCommandValidator : AbstractValidator<RequestDataDeletionCommand>
{
    public RequestDataDeletionCommandValidator()
    {
        RuleFor(x => x.VolunteerId)
            .GreaterThan(0)
            .WithMessage("Valid volunteer ID is required");

        RuleFor(x => x.RequestedByUserId)
            .NotEmpty()
            .WithMessage("RequestedByUserId is required");
    }
}
