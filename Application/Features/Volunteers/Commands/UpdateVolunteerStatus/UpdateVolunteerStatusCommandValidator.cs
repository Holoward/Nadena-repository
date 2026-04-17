using FluentValidation;

namespace Application.Features.Volunteers.Commands.UpdateVolunteerStatus;

public class UpdateVolunteerStatusCommandValidator : AbstractValidator<UpdateVolunteerStatusCommand>
{
    public UpdateVolunteerStatusCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(v => v.Status)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .Must(BeAValidStatus).WithMessage("{PropertyName} must be one of: Registered, Activated, FileReceived, Paid.");
    }

    private bool BeAValidStatus(string status)
    {
        return status == "Registered" || status == "Activated" || status == "FileReceived" || status == "Paid";
    }
}
