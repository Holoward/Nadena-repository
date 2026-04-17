using Domain.Common;

namespace Domain.Entities;

public class YoutubeComment : AuditableBaseEntity
{
    public int VolunteerId { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public string VideoId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int LikeCount { get; set; }
    public bool IsAnonymized { get; set; }
    public string? AnonymizationMethod { get; set; }
}
