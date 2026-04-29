using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces;

namespace Persistence.Services;

public class TakeoutValidationService : ITakeoutValidationService
{
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
                    EarliestRecord = earliest,
                    LatestRecord = latest,
                    DataSourceTypes = string.Join(",", dataSources)
                }
            };
        }
    }

    private static TakeoutValidationResult Fail(string reason) =>
        new() { IsValid = false, FailureReason = reason };
}
