using Domain.Entities;

namespace Application.Interfaces;

public interface ISurveyRepository
{
    Task<SurveyTemplate> AddSurveyTemplateAsync(SurveyTemplate survey);
    Task<SurveyTemplate?> GetSurveyTemplateByIdAsync(int id);
    Task<SurveyTemplate?> GetActiveSurveyTemplateAsync();
    Task AddSurveyResponsesAsync(IEnumerable<SurveyResponse> responses);
    Task<List<SurveyResponse>> GetSurveyResponsesAsync(int surveyTemplateId);
    Task<bool> ContributorHasRespondedAsync(string contributorId, int surveyTemplateId);
    Task UpdateSurveyTemplateAsync(SurveyTemplate survey);
    Task<List<SurveyTemplate>> GetAllSurveyTemplatesAsync();
}