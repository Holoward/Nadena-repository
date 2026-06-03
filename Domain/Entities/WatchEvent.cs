using Domain.Common;

namespace Domain.Entities;

public class WatchEvent : AuditableBaseEntity
{
    /// <summary>Guid of the contributor who uploaded this data.</summary>
    public Guid ContributorId { get; set; }

    /// <summary>SHA-256 hex hash of the YouTube video ID (privacy-preserving).</summary>
    public string VideoIdHash { get; set; } = string.Empty;

    /// <summary>SHA-256 hex hash of the YouTube channel ID (privacy-preserving).</summary>
    public string ChannelIdHash { get; set; } = string.Empty;

    /// <summary>Inferred content category (Tech, Entertainment, News, Gaming, Music, Education, Sports, Cooking, Other).</summary>
    public string Category { get; set; } = "Other";

    /// <summary>UTC timestamp when the video was watched.</summary>
    public DateTime WatchedAt { get; set; }

    /// <summary>Hour of day (0-23) derived from WatchedAt.</summary>
    public int HourOfDay { get; set; }

    /// <summary>Day of week (0 = Monday, 6 = Sunday) derived from WatchedAt.</summary>
    public int DayOfWeek { get; set; }

    /// <summary>Month (1-12) derived from WatchedAt.</summary>
    public int Month { get; set; }

    /// <summary>Year derived from WatchedAt.</summary>
    public int Year { get; set; }

    /// <summary>True if this video was watched before by the same contributor.</summary>
    public bool IsRepeat { get; set; }

    /// <summary>Session identifier (30-minute gap rule).</summary>
    public int SessionId { get; set; }

    /// <summary>1-based position of this watch event within its session.</summary>
    public int PositionInSession { get; set; }
}
