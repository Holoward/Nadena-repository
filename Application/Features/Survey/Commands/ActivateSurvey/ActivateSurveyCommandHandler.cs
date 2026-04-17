using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Survey.Commands.ActivateSurvey;

public class ActivateSurveyCommandHandler : IRequestHandler<ActivateSurveyCommand, ServiceResponse<bool>>
{
    private readonly ISurveyRepository _surveyRepository;

    public ActivateSurveyCommandHandler(ISurveyRepository surveyRepository)
    {
        _surveyRepository = surveyRepository;
    }

    public async Task<ServiceResponse<bool>> Handle(ActivateSurveyCommand request, CancellationToken cancellationToken)
    {
        var survey = await _surveyRepository.GetSurveyTemplateByIdAsync(request.SurveyTemplateId);
        if (survey == null)
        {
            return new ServiceResponse<bool>(false, "Survey not found");
        }

        var allSurveys = await _surveyRepository.GetAllSurveyTemplatesAsync();
        foreach (var s in allSurveys)
        {
            s.IsActive = s.Id == request.SurveyTemplateId;
        }

        foreach (var s in allSurveys)
        {
            await _surveyRepository.UpdateSurveyTemplateAsync(s);
        }

        return new ServiceResponse<bool>(true);
    }
}