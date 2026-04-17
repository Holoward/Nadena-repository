using Application.Features.Volunteers.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service for checking duplicate content in volunteer uploads
/// </summary>
public interface IDeduplicationService
{
    /// <summary>
    /// Checks if new comments contain enough unique content
    /// </summary>
    Task<DeduplicationResultDto> CheckDuplicateAsync(int volunteerId, List<string> newCommentTexts, List<string> newVideoIds);
}
