namespace Application.Features.Datasets.DTOs;

public class DatasetAnalysisDto
{
    public decimal SentimentScore { get; set; }
    public string DominantSentiment { get; set; } = string.Empty;
    public List<string> TopTopics { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
    public int CommentCount { get; set; }
}
