using Application.Features.Survey.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Survey.Queries.GetActiveSurvey;

public class GetActiveSurveyQueryHandler : IRequestHandler<GetActiveSurveyQuery, ServiceResponse<SurveyTemplateDto?>>
{
    private readonly ISurveyRepository _surveyRepository;

    public GetActiveSurveyQueryHandler(ISurveyRepository surveyRepository)
    {
        _surveyRepository = surveyRepository;
    }

    public async Task<ServiceResponse<SurveyTemplateDto?>> Handle(GetActiveSurveyQuery request, CancellationToken cancellationToken)
    {
        var survey = await _surveyRepository.GetActiveSurveyTemplateAsync();
        if (survey == null)
        {
            return new ServiceResponse<SurveyTemplateDto?>(null);
        }

        var dto = new SurveyTemplateDto
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            IsActive = survey.IsActive,
            Version = survey.Version,
            Questions = survey.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q => new SurveyQuestionDto
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

        return new ServiceResponse<SurveyTemplateDto?>(dto);
    }
}