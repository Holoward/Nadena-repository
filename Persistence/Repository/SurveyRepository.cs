using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class SurveyRepository : ISurveyRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SurveyRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SurveyTemplate> AddSurveyTemplateAsync(SurveyTemplate survey)
    {
        await _dbContext.SurveyTemplates.AddAsync(survey);
        await _dbContext.SaveChangesAsync();
        return survey;
    }

    public async Task<SurveyTemplate?> GetSurveyTemplateByIdAsync(int id)
    {
        return await _dbContext.SurveyTemplates
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<SurveyTemplate?> GetActiveSurveyTemplateAsync()
    {
        return await _dbContext.SurveyTemplates
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.IsActive);
    }

    public async Task AddSurveyResponsesAsync(IEnumerable<SurveyResponse> responses)
    {
        await _dbContext.SurveyResponses.AddRangeAsync(responses);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<SurveyResponse>> GetSurveyResponsesAsync(int surveyTemplateId)
    {
        return await _dbContext.SurveyResponses
            .Where(r => r.SurveyTemplateId == surveyTemplateId)
            .ToListAsync();
    }

    public async Task<bool> ContributorHasRespondedAsync(string contributorId, int surveyTemplateId)
    {
        return await _dbContext.SurveyResponses
            .AnyAsync(r => r.ContributorId == contributorId && r.SurveyTemplateId == surveyTemplateId);
    }

    public async Task UpdateSurveyTemplateAsync(SurveyTemplate survey)
    {
        _dbContext.SurveyTemplates.Update(survey);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<SurveyTemplate>> GetAllSurveyTemplatesAsync()
    {
        return await _dbContext.SurveyTemplates
            .Include(s => s.Questions)
            .ToListAsync();
    }
}