namespace Application.Interfaces;

public class TakeoutValidationResult
{
    public bool IsValid { get; set; }
    public string? FailureReason { get; set; }
    public string? GoogleAccountIdHash { get; set; }
    public AnonymizedTakeoutPayload? Payload { get; set; }
}

public class AnonymizedTakeoutPayload
{
    public int TotalWatchEvents { get; set; }
    public Dictionary<string, int> CategoryDistribution { get; set; } = new();
    public Dictionary<int, int> HourOfDayDistribution { get; set; } = new();
    public Dictionary<string, int> DayOfWeekDistribution { get; set; } = new();
    public int TotalSpotifyTracks { get; set; }
    public DateTime EarliestRecord { get; set; }
    public DateTime LatestRecord { get; set; }
    public string DataSourceTypes { get; set; } = string.Empty;
}

public interface ITakeoutValidationService
{
    Task<TakeoutValidationResult> ValidateAndExtractAsync(Stream zipStream, string contributorGoogleAccountEmail);
}
