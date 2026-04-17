using Application.Features.Volunteers.DTOs;
using Application.Interfaces;

namespace Persistence.Services;

/// <summary>
/// Service for checking duplicate content in volunteer uploads
/// </summary>
public class DeduplicationService : IDeduplicationService
{
    private readonly IYoutubeCommentRepository _youtubeCommentRepository;

    public DeduplicationService(IYoutubeCommentRepository youtubeCommentRepository)
    {
        _youtubeCommentRepository = youtubeCommentRepository;
    }

    /// <summary>
    /// Checks if new comments contain enough unique content
    /// </summary>
    public async Task<DeduplicationResultDto> CheckDuplicateAsync(int volunteerId, List<string> newCommentTexts, List<string> newVideoIds)
    {
        // Fetch all existing comments for this volunteer
        var existingComments = await _youtubeCommentRepository.GetByVolunteerIdAsync(volunteerId);
        var existingCommentsList = existingComments.ToList();

        // If volunteer has zero previous comments, always accept
        if (!existingCommentsList.Any())
        {
            return new DeduplicationResultDto
            {
                IsAccepted = true,
                NewContentPercentage = 100m,
                NewCommentCount = newCommentTexts.Count,
                DuplicateCommentCount = 0,
                Message = "First upload accepted",
                RequiredNewContentPercent = 60.0m
            };
        }

        // Build HashSet of existing comment texts (normalized: lowercased, whitespace trimmed)
        var existingTexts = new HashSet<string>(
            existingCommentsList.Select(c => NormalizeText(c.CommentText)),
            StringComparer.OrdinalIgnoreCase);

        // Build HashSet of existing video IDs
        var existingVideoIds = new HashSet<string>(
            existingCommentsList.Select(c => c.VideoId ?? string.Empty),
            StringComparer.OrdinalIgnoreCase);

        int duplicateCount = 0;

        // Check each new comment for duplicates
        for (int i = 0; i < newCommentTexts.Count; i++)
        {
            var newText = newCommentTexts[i];
            var newVideoId = i < newVideoIds.Count ? newVideoIds[i] : string.Empty;

            // Normalize the new text
            var normalizedNewText = NormalizeText(newText);

            // Check if text matches exactly
            if (existingTexts.Contains(normalizedNewText))
            {
                duplicateCount++;
                continue;
            }

            // Check if same videoId + same first 50 chars of text
            if (!string.IsNullOrEmpty(newVideoId) && existingVideoIds.Contains(newVideoId))
            {
                var first50Chars = normalizedNewText.Length > 50 
                    ? normalizedNewText[..50] 
                    : normalizedNewText;

                var hasMatchingPrefix = existingCommentsList.Any(c =>
                {
                    var existingNormalized = NormalizeText(c.CommentText);
                    var existingFirst50 = existingNormalized.Length > 50 
                        ? existingNormalized[..50] 
                        : existingNormalized;
                    
                    return c.VideoId == newVideoId && existingFirst50 == first50Chars;
                });

                if (hasMatchingPrefix)
                {
                    duplicateCount++;
                }
            }
        }

        // Calculate new content percentage
        var newCommentCount = newCommentTexts.Count;
        var newContentCount = newCommentCount - duplicateCount;
        var newContentPercentage = newCommentCount > 0 
            ? (decimal)newContentCount / newCommentCount * 100m 
            : 0m;

        // Determine if accepted (60% threshold)
        var isAccepted = newContentPercentage >= 60.0m;

        var result = new DeduplicationResultDto
        {
            IsAccepted = isAccepted,
            NewContentPercentage = newContentPercentage,
            NewCommentCount = newContentCount,
            DuplicateCommentCount = duplicateCount,
            RequiredNewContentPercent = 60.0m
        };

        if (!isAccepted)
        {
            result.Message = $"Your submission contains {duplicateCount} comments we already have. " +
                           $"Please wait until you have at least 60% new YouTube activity before re-submitting. " +
                           $"New content detected: {newContentPercentage:F1}%";
        }
        else
        {
            result.Message = $"Deduplication passed. {newContentPercentage:F1}% new content detected.";
        }

        return result;
    }

    /// <summary>
    /// Normalizes text for comparison: lowercase and trim whitespace
    /// </summary>
    private static string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.ToLowerInvariant().Trim();
    }
}
