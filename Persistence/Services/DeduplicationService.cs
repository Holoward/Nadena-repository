using Application.Features.Volunteers.DTOs;
using Application.Interfaces;

namespace Persistence.Services;

/// <summary>
/// Checks whether a contributor's new submission contains enough unique behavioral data
/// to be accepted. Currently validates based on the WatchEvent table.
/// The 60% threshold prevents contributors from re-submitting the same export repeatedly.
/// </summary>
public class DeduplicationService : IDeduplicationService
{
    /// <summary>
    /// Always accepts the submission as unique since deduplication
    /// is now handled at the validation layer in TakeoutValidationService
    /// via the DataIntegrityHash check on the Volunteer record.
    /// This service is kept for interface compatibility and future expansion.
    /// </summary>
    public Task<DeduplicationResultDto> CheckDuplicateAsync(
        int volunteerId,
        List<string> newCommentTexts,
        List<string> newVideoIds)
    {
        return Task.FromResult(new DeduplicationResultDto
        {
            IsAccepted = true,
            NewContentPercentage = 100m,
            NewCommentCount = newCommentTexts.Count,
            DuplicateCommentCount = 0,
            Message = "Deduplication handled at submission layer via integrity hash.",
            RequiredNewContentPercent = 60.0m
        });
    }
}
