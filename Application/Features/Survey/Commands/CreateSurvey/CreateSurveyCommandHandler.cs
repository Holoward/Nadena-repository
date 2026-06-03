using Application.Features.Survey.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Survey.Commands.CreateSurvey;

public class CreateSurveyCommandHandler : IRequestHandler<CreateSurveyCommand, ServiceResponse<SurveyTemplateDto>>
{
    private readonly ISurveyRepository _surveyRepository;

    public CreateSurveyCommandHandler(ISurveyRepository surveyRepository)
    {
        _surveyRepository = surveyRepository;
    }

    public async Task<ServiceResponse<SurveyTemplateDto>> Handle(CreateSurveyCommand request, CancellationToken cancellationToken)
    {
        var survey = new Domain.Entities.SurveyTemplate
        {
            ResearcherId = request.ResearcherId,
            Title = request.Title,
            Description = request.Description,
            Version = request.Version,
            IsActive = false
        };

        foreach (var q in request.Questions)
        {
            survey.Questions.Add(new Domain.Entities.SurveyQuestion
            {
                OrderIndex = q.OrderIndex,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Options = q.Options,
                ScaleMin = q.ScaleMin,
                ScaleMax = q.ScaleMax,
                ScaleMinLabel = q.ScaleMinLabel,
                ScaleMaxLabel = q.ScaleMaxLabel,
                IsRequired = q.IsRequired
            });
        }

        var created = await _surveyRepository.AddSurveyTemplateAsync(survey);

        var dto = new SurveyTemplateDto
        {
            Id = created.Id,
            Title = created.Title,
            Description = created.Description,
            IsActive = created.IsActive,
            Version = created.Version,
            Questions = created.Questions.Select(q => new SurveyQuestionDto
            {
                Id = q.Id,
                OrderIndex = q.OrderIndex,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Options = q.Options,
                ScaleMin = q.ScaleMin,
                ScaleMax = q.ScaleMax,
                ScaleMinLabel = q.ScaleMinLabel,
                ScaleMaxLabel = q.ScaleMaxLabel,
                IsRequired = q.IsRequired
            }).ToList()
        };

        return new ServiceResponse<SurveyTemplateDto>(dto);
    }
}