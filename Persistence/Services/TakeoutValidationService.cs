using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces;

namespace Persistence.Services;

/// <summary>
/// Validates Google Takeout ZIP files submitted by contributors. Checks structure, schema, timestamp plausibility, and hashes the contributor's Google account ID for deduplication.
/// </summary>
public class TakeoutValidationService : ITakeoutValidationService
{
    /// <summary>
    /// Validates a Takeout ZIP stream and extracts an anonymized summary payload when the file is acceptable.
    /// </summary>
    public async Task<TakeoutValidationResult> ValidateAndExtractAsync(
        Stream zipStream,
        string contributorGoogleAccountEmail)
    {
        using var ms = new MemoryStream();
        await zipStream.CopyToAsync(ms);
        ms.Position = 0;

        ZipArchive archive;
        try
        {
            archive = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: true);
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
            using var watchReader = new StreamReader(watchStream);
            var watchJson = await watchReader.ReadToEndAsync();

            JsonDocument watchDoc;
            try { watchDoc = JsonDocument.Parse(watchJson); }
            catch { return Fail("Watch history file could not be parsed. It may be corrupted."); }

            if (watchDoc.RootElement.ValueKind != JsonValueKind.Array)
                return Fail("Watch history format is not recognized.");

            var validEntries = watchDoc.RootElement.EnumerateArray()
                .Where(e =>
                    e.TryGetProperty("header", out _) &&
                    e.TryGetProperty("time", out var t) &&
                    DateTime.TryParse(t.GetString(), out _))
                .ToList();

            if (validEntries.Count < 10)
                return Fail("Watch history contains fewer than 10 records. Please export at least 30 days of history.");

            var timestamps = validEntries
                .Select(e => DateTime.Parse(e.GetProperty("time").GetString()!))
                .OrderBy(t => t)
                .ToList();

            var earliest = timestamps.First();
            var latest = timestamps.Last();

            if ((latest - earliest).TotalDays < 7)
                return Fail("Watch history spans less than 7 days. Please include at least 30 days of history.");

            if (timestamps.All(t => (t - earliest).TotalHours <= 24))
                return Fail("Watch history timestamps are implausibly concentrated. Possible fabricated data.");

            var hashBytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(contributorGoogleAccountEmail.ToLowerInvariant().Trim()));
            var accountIdHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var categoryDist = validEntries
                .GroupBy(e => e.GetProperty("header").GetString() ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            var hourDist = timestamps
                .GroupBy(t => t.Hour)
                .ToDictionary(g => g.Key, g => g.Count());

            var dayDist = timestamps
                .GroupBy(t => t.DayOfWeek.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            var spotifyEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.Contains("StreamingHistory", StringComparison.OrdinalIgnoreCase));

            var totalSpotifyTracks = 0;
            var dataSources = new List<string> { "YouTube" };

            if (spotifyEntry != null)
            {
                using var spotifyStream = spotifyEntry.Open();
                using var spotifyReader = new StreamReader(spotifyStream);
                var spotifyJson = await spotifyReader.ReadToEndAsync();
                try
                {
                    var spotifyDoc = JsonDocument.Parse(spotifyJson);
                    if (spotifyDoc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        totalSpotifyTracks = spotifyDoc.RootElement.GetArrayLength();
                        dataSources.Add("Spotify");
                    }
                }
                catch { /* non-fatal */ }
            }

            var netflixEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.Contains("ViewingActivity", StringComparison.OrdinalIgnoreCase) &&
                e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

            var totalNetflixSessions = 0;
            var netflixDeviceTypes = new Dictionary<string, int>();
            var netflixSessionMinutes = new List<double>();

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
                                    netflixSessionMinutes.Add(dur.TotalMinutes);
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
                    TotalWatchEvents = validEntries.Count,
                    CategoryDistribution = categoryDist,
                    HourOfDayDistribution = hourDist,
                    DayOfWeekDistribution = dayDist,
                    TotalSpotifyTracks = totalSpotifyTracks,
                    TotalNetflixSessions = totalNetflixSessions,
                    NetflixDeviceTypeDistribution = netflixDeviceTypes,
                    AverageNetflixSessionMinutes = netflixSessionMinutes.Count > 0
                        ? Math.Round(netflixSessionMinutes.Average(), 2)
                        : 0,
                    EarliestRecord = earliest,
                    LatestRecord = latest,
                    DataSourceTypes = string.Join(",", dataSources)
                }
            };
        }
    }

    /// <summary>
    /// Creates a failed validation result with the supplied reason.
    /// </summary>
    private static TakeoutValidationResult Fail(string reason) =>
        new() { IsValid = false, FailureReason = reason };
}
