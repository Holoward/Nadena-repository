using Application.Features.Survey.Commands.CreateSurvey;
using Application.Features.Survey.DTOs;
using FluentValidation;

namespace Application.Features.Survey.Commands.CreateSurvey;

public class CreateSurveyCommandValidator : AbstractValidator<CreateSurveyCommand>
{
    public CreateSurveyCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000);

        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("Version is required")
            .Matches(@"^\d+\.\d+$").WithMessage("Version must be in format like '1.0'");

        RuleFor(x => x.Questions)
            .NotEmpty().WithMessage("At least one question is required");

        RuleForEach(x => x.Questions).ChildRules(question =>
        {
            question.RuleFor(q => q.QuestionText)
                .NotEmpty().WithMessage("Question text is required")
                .MaximumLength(500);

            question.RuleFor(q => q.QuestionType)
                .IsInEnum();

            question.RuleFor(q => q.OrderIndex)
                .GreaterThanOrEqualTo(0);

            question.RuleFor(q => q.Options)
                .NotEmpty()
                .When(q => q.QuestionType == Domain.Enums.SurveyQuestionType.SingleChoice || 
                           q.QuestionType == Domain.Enums.SurveyQuestionType.MultiChoice)
                .WithMessage("Options are required for SingleChoice and MultiChoice questions");

            question.RuleFor(q => q.ScaleMin)
                .GreaterThan(0)
                .When(q => q.QuestionType == Domain.Enums.SurveyQuestionType.Scale)
                .WithMessage("ScaleMin must be greater than 0");

            question.RuleFor(q => q.ScaleMax)
                .GreaterThan(0)
                .When(q => q.QuestionType == Domain.Enums.SurveyQuestionType.Scale)
                .WithMessage("ScaleMax must be greater than 0");

            question.RuleFor(q => q.ScaleMin)
                .LessThan(q => q.ScaleMax)
                .When(q => q.QuestionType == Domain.Enums.SurveyQuestionType.Scale && q.ScaleMin.HasValue && q.ScaleMax.HasValue)
                .WithMessage("ScaleMin must be less than ScaleMax");
        });
    }
}