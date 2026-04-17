using Application.Features.Survey.Commands.ActivateSurvey;
using FluentValidation;

namespace Application.Features.Survey.Commands.ActivateSurvey;

public class ActivateSurveyCommandValidator : AbstractValidator<ActivateSurveyCommand>
{
    public ActivateSurveyCommandValidator()
    {
        RuleFor(x => x.SurveyTemplateId)
            .GreaterThan(0).WithMessage("Valid survey ID is required");
    }
}