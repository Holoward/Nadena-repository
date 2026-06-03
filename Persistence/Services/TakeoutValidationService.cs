using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Persistence.Services;

/// <summary>
/// Validates Google Takeout ZIP files submitted by contributors. Checks structure, schema, timestamp plausibility, and hashes the contributor's Google account ID for deduplication.
/// </summary>
public class TakeoutValidationService : ITakeoutValidationService
{
    private readonly IConfiguration _configuration;

    public TakeoutValidationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    private class WatchEventDto
    {
        public string? Header { get; set; }
        public string? Time { get; set; }
    }

    private static readonly SemaphoreSlim _extractionSemaphore = new SemaphoreSlim(3, 3);

    /// <summary>
    /// Validates a Takeout ZIP stream and extracts an anonymized summary payload when the file is acceptable.
    /// </summary>
    public async Task<TakeoutValidationResult> ValidateAndExtractAsync(
        Stream zipStream,
        string contributorGoogleAccountEmail)
    {
        await _extractionSemaphore.WaitAsync();
        try
        {
            var tempFilePath = Path.GetTempFileName();
            try
        {
        using (var tempFile = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            await zipStream.CopyToAsync(tempFile);

        ZipArchive archive;
        try
        {
            archive = new ZipArchive(
                new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read),
                ZipArchiveMode.Read);
        }
        catch
        {
            return Fail("File is not a valid ZIP archive.");
        }

        using (archive)
        {
            var watchEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.Contains("watch-history.json", StringComparison.OrdinalIgnoreCase));

            if (watchEntry == null)
                return Fail("Missing YouTube watch history. Make sure you selected YouTube in Google Takeout.");

            using var watchStream = watchEntry.Open();

            // Stream entries without buffering — track only aggregates needed downstream
            int validCount = 0;
            DateTime? earliest = null;
            DateTime? latest = null;
            var categoryDist = new Dictionary<string, int>();
            var hourDist = new Dictionary<int, int>();
            var dayDist = new Dictionary<string, int>();
            await foreach (var entry in JsonSerializer.DeserializeAsyncEnumerable<WatchEventDto>(
                watchStream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
            {
                if (entry?.Header != null
                    && entry.Time != null
                    && DateTime.TryParse(entry.Time, out var ts))
                {
                    validCount++;
                    if (earliest == null || ts < earliest) earliest = ts;
                    if (latest == null || ts > latest) latest = ts;
                    var cat = entry.Header ?? "Unknown";
                    categoryDist[cat] = categoryDist.GetValueOrDefault(cat) + 1;
                    hourDist[ts.Hour] = hourDist.GetValueOrDefault(ts.Hour) + 1;
                    var dow = ts.DayOfWeek.ToString();
                    dayDist[dow] = dayDist.GetValueOrDefault(dow) + 1;
                }
            }

            if (validCount < 10)
                return Fail("Watch history contains fewer than 10 records. Please export at least 30 days of history.");

            if (earliest == null || latest == null || (latest.Value - earliest.Value).TotalDays < 7)
                return Fail("Watch history spans less than 7 days. Please include at least 30 days of history.");

            if (earliest != null && latest != null && (latest.Value - earliest.Value).TotalHours <= 24)
                return Fail("Watch history timestamps are implausibly concentrated. Possible fabricated data.");

            var normalizedEmail = Application.Common.InputSanitizer.NormalizeEmailForDeduplication(contributorGoogleAccountEmail);
            var saltString = _configuration["NadenaSettings:HashSalt"] ?? "default_secure_salt_fallback_do_not_use_in_prod";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(saltString));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalizedEmail));
            var accountIdHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var spotifyEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.Contains("StreamingHistory", StringComparison.OrdinalIgnoreCase));

            var totalSpotifyTracks = 0;
            var dataSources = new List<string> { "YouTube" };

            if (spotifyEntry != null)
            {
                try
                {
                    using var spotifyStream = spotifyEntry.Open();
                    // Stream-count without loading the full JSON into memory
                    await foreach (var _ in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(
                        spotifyStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
                    {
                        totalSpotifyTracks++;
                    }
                    if (totalSpotifyTracks > 0)
                        dataSources.Add("Spotify");
                }
                catch { /* non-fatal */ }
            }

            var netflixEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.Contains("ViewingActivity", StringComparison.OrdinalIgnoreCase) &&
                e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

            var totalNetflixSessions = 0;
            var netflixDeviceTypes = new Dictionary<string, int>();
            double totalNetflixSessionMinutes = 0;
            int validNetflixDurationsCount = 0;

            if (netflixEntry != null)
            {
                try
                {
                    using var netflixStream = netflixEntry.Open();
                    using var netflixReader = new StreamReader(netflixStream);
                    var header = await netflixReader.ReadLineAsync();
                    if (header != null)
                    {
                        var headerCols = header.Split(',');
                        var durationIdx = Array.FindIndex(headerCols, h => h.Trim('"').Equals("Duration", StringComparison.OrdinalIgnoreCase));
                        var deviceIdx = Array.FindIndex(headerCols, h => h.Trim('"').Equals("Device Type", StringComparison.OrdinalIgnoreCase));

                        string? line;
                        while ((line = await netflixReader.ReadLineAsync()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var cols = line.Split(',');
                            totalNetflixSessions++;

                            if (deviceIdx >= 0 && deviceIdx < cols.Length)
                            {
                                var device = cols[deviceIdx].Trim('"').Trim();
                                if (!string.IsNullOrEmpty(device))
                                {
                                    var deviceKey = device.Length > 30 ? device[..30] : device;
                                    netflixDeviceTypes[deviceKey] = netflixDeviceTypes.GetValueOrDefault(deviceKey) + 1;
                                }
                            }

                            if (durationIdx >= 0 && durationIdx < cols.Length)
                            {
                                var durStr = cols[durationIdx].Trim('"').Trim();
                                if (TimeSpan.TryParse(durStr, out var dur))
                                {
                                    totalNetflixSessionMinutes += dur.TotalMinutes;
                                    validNetflixDurationsCount++;
                                }
                            }
                        }

                        if (totalNetflixSessions > 0)
                            dataSources.Add("Netflix");
                    }
                }
                catch { /* non-fatal — Netflix export is optional */ }
            }

            return new TakeoutValidationResult
            {
                IsValid = true,
                GoogleAccountIdHash = accountIdHash,
                Payload = new AnonymizedTakeoutPayload
                {
                    TotalWatchEvents = validCount,
                    CategoryDistribution = categoryDist,
                    HourOfDayDistribution = hourDist,
                    DayOfWeekDistribution = dayDist,
                    TotalSpotifyTracks = totalSpotifyTracks,
                    TotalNetflixSessions = totalNetflixSessions,
                    NetflixDeviceTypeDistribution = netflixDeviceTypes,
                    AverageNetflixSessionMinutes = validNetflixDurationsCount > 0
                        ? Math.Round(totalNetflixSessionMinutes / validNetflixDurationsCount, 2)
                        : 0,
                    EarliestRecord = earliest ?? DateTime.MinValue,
                    LatestRecord = latest ?? DateTime.MinValue,
                    DataSourceTypes = string.Join(",", dataSources)
                }
            };
        }
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
        }
        finally
        {
            _extractionSemaphore.Release();
        }
    }

    /// <summary>
    /// Creates a failed validation result with the supplied reason.
    /// </summary>
    private static TakeoutValidationResult Fail(string reason) =>
        new() { IsValid = false, FailureReason = reason };
}
