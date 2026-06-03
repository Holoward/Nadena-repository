using Application.Features.Survey.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Survey.Queries.ExportSurveyResponses;

public class ExportSurveyResponsesQueryHandler : IRequestHandler<ExportSurveyResponsesQuery, ServiceResponse<List<SurveyExportRow>>>
{
    private readonly ISurveyRepository _surveyRepository;

    public ExportSurveyResponsesQueryHandler(ISurveyRepository surveyRepository)
    {
        _surveyRepository = surveyRepository;
    }

    public async Task<ServiceResponse<List<SurveyExportRow>>> Handle(ExportSurveyResponsesQuery request, CancellationToken cancellationToken)
    {
        var survey = await _surveyRepository.GetSurveyTemplateByIdAsync(request.SurveyTemplateId);
        if (survey == null)
        {
            return new ServiceResponse<List<SurveyExportRow>>(null, "Survey not found");
        }

        var responses = await _surveyRepository.GetSurveyResponsesAsync(request.SurveyTemplateId);
        
        var questionDict = survey.Questions.ToDictionary(q => q.Id);

        var exportRows = responses.Select(r =>
        {
            var question = questionDict.GetValueOrDefault(r.QuestionId);
            return new SurveyExportRow
            {
                ContributorId = r.ContributorId,
                QuestionText = question?.QuestionText ?? "Unknown",
                QuestionType = question?.QuestionType.ToString() ?? "Unknown",
                ResponseValue = r.ResponseValue,
                RespondedAt = r.RespondedAt
            };
        }).ToList();

        return new ServiceResponse<List<SurveyExportRow>>(exportRows);
    }
}