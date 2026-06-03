using Application.Features.Survey.Commands.SubmitSurveyResponse;
using Application.Features.Survey.DTOs;
using FluentValidation;

namespace Application.Features.Survey.Commands.SubmitSurveyResponse;

public class SubmitSurveyResponseCommandValidator : AbstractValidator<SubmitSurveyResponseCommand>
{
    public SubmitSurveyResponseCommandValidator()
    {
        RuleFor(x => x.ContributorId)
            .NotEmpty().WithMessage("Contributor ID is required");

        RuleFor(x => x.SurveyTemplateId)
            .GreaterThan(0).WithMessage("Survey template ID is required");

        RuleFor(x => x.Responses)
            .NotEmpty().WithMessage("At least one response is required");

        RuleForEach(x => x.Responses).ChildRules(response =>
        {
            response.RuleFor(r => r.QuestionId)
                .GreaterThan(0).WithMessage("Question ID is required");

            response.RuleFor(r => r.ResponseValue)
                .NotEmpty().WithMessage("Response value is required");
        });
    }
}