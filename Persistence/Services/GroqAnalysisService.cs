using System.Text;
using System.Text.Json;
using Application.Features.Datasets.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Persistence.Services;

/// <summary>
/// Service for analyzing comments using Groq AI
/// </summary>
public class GroqAnalysisService : IGroqAnalysisService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GroqAnalysisService> _logger;

    public GroqAnalysisService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GroqAnalysisService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes comments and returns sentiment and topic analysis
    /// </summary>
    public async Task<DatasetAnalysisDto> AnalyzeCommentsAsync(List<string> comments, string contentCategory)
    {
        var groqApiKey = _configuration["NadenaSettings:GroqApiKey"];
        var groqModel = _configuration["NadenaSettings:GroqModel"] ?? "llama3-8b-8192";

        if (string.IsNullOrEmpty(groqApiKey))
        {
            return new DatasetAnalysisDto
            {
                Summary = "Groq API key not configured. Add NadenaSettings:GroqApiKey to appsettings.json",
                AnalyzedAt = DateTime.UtcNow,
                CommentCount = comments.Count
            };
        }

        try
        {
            var client = _httpClientFactory.CreateClient("GroqClient");

            var commentsText = string.Join("\n", comments.Take(50));
            var prompt = $"Analyze these YouTube comments and return ONLY a JSON object with no extra text: {{\"sentimentScore\": 0.0-1.0, \"dominantSentiment\": \"Positive|Negative|Neutral|Mixed\", \"topTopics\": [\"topic1\",\"topic2\",\"topic3\"], \"summary\": \"one paragraph for buyers\"}}. Comments: {commentsText}";

            var requestBody = new
            {
                model = groqModel,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.3,
                max_tokens = 500
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = jsonContent
            };
            request.Headers.Add("Authorization", $"Bearer {groqApiKey}");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var messageContent = responseObject
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(messageContent))
            {
                return new DatasetAnalysisDto
                {
                    Summary = "Analysis failed. Empty response from Groq API.",
                    AnalyzedAt = DateTime.UtcNow,
                    CommentCount = comments.Count
                };
            }

            // Parse the JSON response from Groq
            var analysisResult = JsonSerializer.Deserialize<JsonElement>(messageContent);

            var sentimentScore = analysisResult.TryGetProperty("sentimentScore", out var scoreElement)
                ? scoreElement.GetDecimal()
                : 0.5m;

            var dominantSentiment = analysisResult.TryGetProperty("dominantSentiment", out var sentimentElement)
                ? sentimentElement.GetString() ?? "Neutral"
                : "Neutral";

            var topTopics = new List<string>();
            if (analysisResult.TryGetProperty("topTopics", out var topicsElement))
            {
                foreach (var topic in topicsElement.EnumerateArray())
                {
                    var topicValue = topic.GetString();
                    if (!string.IsNullOrEmpty(topicValue))
                    {
                        topTopics.Add(topicValue);
                    }
                }
            }

            var summary = analysisResult.TryGetProperty("summary", out var summaryElement)
                ? summaryElement.GetString() ?? "Analysis completed successfully."
                : "Analysis completed successfully.";

            return new DatasetAnalysisDto
            {
                SentimentScore = sentimentScore,
                DominantSentiment = dominantSentiment,
                TopTopics = topTopics,
                Summary = summary,
                AnalyzedAt = DateTime.UtcNow,
                CommentCount = comments.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing comments with Groq API");
            return new DatasetAnalysisDto
            {
                Summary = "Analysis failed. Check Groq API key and try again.",
                AnalyzedAt = DateTime.UtcNow,
                CommentCount = comments.Count
            };
        }
    }
}
