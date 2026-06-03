using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Survey.Commands.SubmitSurveyResponse;

public class SubmitSurveyResponseCommandHandler : IRequestHandler<SubmitSurveyResponseCommand, ServiceResponse<bool>>
{
    private readonly ISurveyRepository _surveyRepository;

    public SubmitSurveyResponseCommandHandler(ISurveyRepository surveyRepository)
    {
        _surveyRepository = surveyRepository;
    }

    public async Task<ServiceResponse<bool>> Handle(SubmitSurveyResponseCommand request, CancellationToken cancellationToken)
    {
        var hasResponded = await _surveyRepository.ContributorHasRespondedAsync(request.ContributorId, request.SurveyTemplateId);
        if (hasResponded)
        {
            return new ServiceResponse<bool>(false, "You have already completed this survey.");
        }

        var survey = await _surveyRepository.GetSurveyTemplateByIdAsync(request.SurveyTemplateId);
        if (survey == null)
        {
            return new ServiceResponse<bool>(false, "Survey not found.");
        }

        var questions = survey.Questions.ToDictionary(q => q.Id);
        var requiredQuestionIds = questions.Values.Where(q => q.IsRequired).Select(q => q.Id).ToHashSet();
        var answeredQuestionIds = request.Responses.Select(r => r.QuestionId).ToHashSet();

        var missingRequired = requiredQuestionIds.Except(answeredQuestionIds).ToList();
        if (missingRequired.Any())
        {
            return new ServiceResponse<bool>(false, "Please answer all required questions.");
        }

        var responses = request.Responses.Select(r => new Domain.Entities.SurveyResponse
        {
            ContributorId = request.ContributorId,
            SurveyTemplateId = request.SurveyTemplateId,
            QuestionId = r.QuestionId,
            ResponseValue = r.ResponseValue,
            RespondedAt = DateTime.UtcNow
        }).ToList();

        await _surveyRepository.AddSurveyResponsesAsync(responses);

        return new ServiceResponse<bool>(true);
    }
}