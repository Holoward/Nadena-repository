namespace Application.Interfaces;

/// <summary>
/// Represents the outcome of validating and extracting a contributor's Takeout ZIP file.
/// </summary>
public class TakeoutValidationResult
{
    public bool IsValid { get; set; }
    public string? FailureReason { get; set; }
    public string? GoogleAccountIdHash { get; set; }
    public AnonymizedTakeoutPayload? Payload { get; set; }
}

/// <summary>
/// Represents the anonymized, aggregate data extracted from a contributor's Takeout export.
/// </summary>
public class AnonymizedTakeoutPayload
{
    public int TotalWatchEvents { get; set; }
    public Dictionary<string, int> CategoryDistribution { get; set; } = new();
    public Dictionary<int, int> HourOfDayDistribution { get; set; } = new();
    public Dictionary<string, int> DayOfWeekDistribution { get; set; } = new();
    public int TotalSpotifyTracks { get; set; }
    public int TotalNetflixSessions { get; set; }
    public Dictionary<string, int> NetflixDeviceTypeDistribution { get; set; } = new();
    public double AverageNetflixSessionMinutes { get; set; }
    public DateTime EarliestRecord { get; set; }
    public DateTime LatestRecord { get; set; }
    public string DataSourceTypes { get; set; } = string.Empty;
}

/// <summary>
/// Defines validation and extraction behavior for Google Takeout ZIP submissions.
/// </summary>
public interface ITakeoutValidationService
{
    /// <summary>
    /// Validates a Takeout ZIP stream and returns an anonymized payload when validation succeeds.
    /// </summary>
    Task<TakeoutValidationResult> ValidateAndExtractAsync(Stream zipStream, string contributorGoogleAccountEmail);
}
