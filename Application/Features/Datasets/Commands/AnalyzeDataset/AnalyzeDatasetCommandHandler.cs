using System.Text.Json;
using Application.Features.Datasets.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Datasets.Commands.AnalyzeDataset;

public class AnalyzeDatasetCommandHandler : IRequestHandler<AnalyzeDatasetCommand, ServiceResponse<DatasetAnalysisDto>>
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IYoutubeCommentRepository _youtubeCommentRepository;
    private readonly IGroqAnalysisService _groqAnalysisService;

    public AnalyzeDatasetCommandHandler(
        IDatasetRepository datasetRepository,
        IYoutubeCommentRepository youtubeCommentRepository,
        IGroqAnalysisService groqAnalysisService)
    {
        _datasetRepository = datasetRepository;
        _youtubeCommentRepository = youtubeCommentRepository;
        _groqAnalysisService = groqAnalysisService;
    }

    public async Task<ServiceResponse<DatasetAnalysisDto>> Handle(AnalyzeDatasetCommand request, CancellationToken cancellationToken)
    {
        var dataset = await _datasetRepository.GetByIdAsync(request.DatasetId);
        if (dataset == null)
        {
            return new ServiceResponse<DatasetAnalysisDto>("Dataset not found");
        }

        // Fetch all YouTube comments for the volunteers in this dataset
        var comments = await _youtubeCommentRepository.ListAsync();
        var commentTexts = comments
            .Where(c => !string.IsNullOrEmpty(c.CommentText))
            .Select(c => c.CommentText)
            .Take(50)
            .ToList();

        if (!commentTexts.Any())
        {
            return new ServiceResponse<DatasetAnalysisDto>("No comments found for analysis");
        }

        // Analyze comments using Groq AI
        var analysisResult = await _groqAnalysisService.AnalyzeCommentsAsync(commentTexts, "YouTube");

        // Serialize result to JSON and store in Dataset
        dataset.AnalysisResult = JsonSerializer.Serialize(analysisResult);
        await _datasetRepository.UpdateAsync(dataset);

        return new ServiceResponse<DatasetAnalysisDto>(analysisResult);
    }
}
