using Application.Features.Datasets.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service for analyzing comments using Groq AI
/// </summary>
public interface IGroqAnalysisService
{
    /// <summary>
    /// Analyzes comments and returns sentiment and topic analysis
    /// </summary>
    Task<DatasetAnalysisDto> AnalyzeCommentsAsync(List<string> comments, string contentCategory);
}
