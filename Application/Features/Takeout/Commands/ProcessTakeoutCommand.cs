using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;

namespace Application.Features.Takeout.Commands;

public sealed class CategoryCountDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class TakeoutSummaryDto
{
    public int TotalVideos { get; set; }
    public int UniqueVideos { get; set; }
    public int UniqueChannels { get; set; }
    public TakeoutDateRangeDto DateRange { get; set; } = new();
    public List<CategoryCountDto> TopCategories { get; set; } = new();
}

public sealed class TakeoutDateRangeDto
{
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
}

internal sealed class TakeoutSubtitle
{
    public string? Name { get; set; }
    public string? Url { get; set; }
}

internal sealed class TakeoutRecord
{
    public string? Header { get; set; }
    public string? Title { get; set; }
    public string? TitleUrl { get; set; }
    public List<TakeoutSubtitle>? Subtitles { get; set; }
    public string? Time { get; set; }
}

internal sealed record ParsedTakeoutRecord(
    string Title,
    string VideoId,
    string? ChannelId,
    DateTime WatchedAtUtc);

public sealed class ProcessTakeoutCommand : IRequest<ServiceResponse<TakeoutSummaryDto>>
{
    public Stream ZipFileStream { get; init; } = Stream.Null;
    public Guid ContributorId { get; init; }
}

public sealed class ProcessTakeoutCommandHandler
    : IRequestHandler<ProcessTakeoutCommand, ServiceResponse<TakeoutSummaryDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    private static readonly (string Category, string[] Keywords)[] CategoryRules =
    [
        ("Tech", ["tech", "programming", "code", "developer", "software", "hardware", "computer", "ai", "machine learning", "javascript", "python", "linux", "tutorial", "api", "framework", "dotnet", ".net", "react", "angular"]),
        ("Entertainment", ["entertainment", "movie", "film", "trailer", "series", "comedy", "funny", "vlog", "challenge", "reaction", "podcast", "interview", "celebrity", "prank", "sketch", "animation", "anime", "drama"]),
        ("News", ["news", "breaking", "report", "politics", "election", "cnn", "bbc", "fox", "msnbc", "journalist", "headline", "investigation", "debate"]),
        ("Gaming", ["gaming", "gameplay", "game", "playthrough", "walkthrough", "esports", "minecraft", "fortnite", "gta", "valorant", "twitch", "lets play", "speedrun", "xbox", "playstation", "nintendo"]),
        ("Music", ["music", "song", "album", "concert", "lyrics", "remix", "playlist", "official video", "official audio", "live performance", "mv", "beat", "instrumental", "karaoke"]),
        ("Education", ["education", "lecture", "course", "learn", "study", "university", "professor", "class", "lesson", "explained", "how to", "khan academy", "ted", "documentary"]),
        ("Sports", ["sports", "football", "soccer", "basketball", "nba", "nfl", "mlb", "tennis", "cricket", "highlights", "goal", "match", "championship", "olympic", "fifa", "ufc"]),
        ("Cooking", ["cooking", "recipe", "food", "chef", "baking", "kitchen", "meal", "cuisine", "restaurant", "bbq", "grill", "ingredients"])
    ];

    private const string WatchHistoryPath = "Takeout/YouTube and YouTube Music/history/watch-history.json";
    private static readonly TimeSpan SessionGap = TimeSpan.FromMinutes(30);

    private readonly IWatchEventRepository _watchEventRepository;

    public ProcessTakeoutCommandHandler(IWatchEventRepository watchEventRepository)
    {
        _watchEventRepository = watchEventRepository;
    }

    public async Task<ServiceResponse<TakeoutSummaryDto>> Handle(
        ProcessTakeoutCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ZipFileStream == Stream.Null)
        {
            return new ServiceResponse<TakeoutSummaryDto>("A non-empty ZIP file is required.");
        }

        if (request.ContributorId == Guid.Empty)
        {
            return new ServiceResponse<TakeoutSummaryDto>("A valid contributorId is required.");
        }

        List<TakeoutRecord>? rawRecords;

        try
        {
            await using var bufferedZipStream = new MemoryStream();
            await request.ZipFileStream.CopyToAsync(bufferedZipStream, cancellationToken);
            bufferedZipStream.Position = 0;

            using var archive = new ZipArchive(bufferedZipStream, ZipArchiveMode.Read, leaveOpen: false);
            var watchHistoryEntry = FindWatchHistoryEntry(archive);

            if (watchHistoryEntry is null)
            {
                return new ServiceResponse<TakeoutSummaryDto>(
                    "watch-history.json was not found in the uploaded Google Takeout archive.");
            }

            await using var entryStream = watchHistoryEntry.Open();
            rawRecords = await JsonSerializer.DeserializeAsync<List<TakeoutRecord>>(
                entryStream,
                JsonOptions,
                cancellationToken);
        }
        catch (InvalidDataException)
        {
            return new ServiceResponse<TakeoutSummaryDto>("The uploaded file is not a valid ZIP archive.");
        }
        catch (JsonException)
        {
            return new ServiceResponse<TakeoutSummaryDto>("watch-history.json could not be parsed.");
        }

        if (rawRecords is null || rawRecords.Count == 0)
        {
            return new ServiceResponse<TakeoutSummaryDto>("watch-history.json did not contain any records.");
        }

        var parsedRecords = rawRecords
            .Select(TryParseRecord)
            .OfType<ParsedTakeoutRecord>()
            .OrderBy(record => record.WatchedAtUtc)
            .ToList();

        if (parsedRecords.Count == 0)
        {
            return new ServiceResponse<TakeoutSummaryDto>(
                "No valid YouTube watch events were found in watch-history.json.");
        }

        var watchEvents = new List<WatchEvent>(parsedRecords.Count);
        var seenVideoIds = new HashSet<string>(StringComparer.Ordinal);
        var uniqueVideoHashes = new HashSet<string>(StringComparer.Ordinal);
        var uniqueChannelHashes = new HashSet<string>(StringComparer.Ordinal);

        var currentSessionId = 0;
        var positionInSession = 0;
        DateTime? previousWatchedAt = null;

        foreach (var record in parsedRecords)
        {
            if (previousWatchedAt is null || record.WatchedAtUtc - previousWatchedAt > SessionGap)
            {
                currentSessionId++;
                positionInSession = 0;
            }

            positionInSession++;

            var videoIdHash = ComputeSha256(record.VideoId);
            var channelIdHash = string.IsNullOrWhiteSpace(record.ChannelId)
                ? string.Empty
                : ComputeSha256(record.ChannelId);

            uniqueVideoHashes.Add(videoIdHash);
            if (!string.IsNullOrWhiteSpace(channelIdHash))
            {
                uniqueChannelHashes.Add(channelIdHash);
            }

            watchEvents.Add(new WatchEvent
            {
                ContributorId = request.ContributorId,
                VideoIdHash = videoIdHash,
                ChannelIdHash = channelIdHash,
                Category = InferCategory(record.Title),
                WatchedAt = record.WatchedAtUtc,
                HourOfDay = record.WatchedAtUtc.Hour,
                DayOfWeek = GetMondayBasedDayOfWeek(record.WatchedAtUtc),
                Month = record.WatchedAtUtc.Month,
                Year = record.WatchedAtUtc.Year,
                IsRepeat = !seenVideoIds.Add(record.VideoId),
                SessionId = currentSessionId,
                PositionInSession = positionInSession
            });

            previousWatchedAt = record.WatchedAtUtc;
        }

        await _watchEventRepository.ReplaceForContributorAsync(
            request.ContributorId,
            watchEvents,
            cancellationToken);

        var categoryCounts = watchEvents
            .GroupBy(e => e.Category)
            .Select(g => new CategoryCountDto
            {
                Category = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(c => c.Count)
            .Take(3)
            .ToList();

        var summary = new TakeoutSummaryDto
        {
            TotalVideos = watchEvents.Count,
            UniqueVideos = uniqueVideoHashes.Count,
            UniqueChannels = uniqueChannelHashes.Count,
            DateRange = new TakeoutDateRangeDto
            {
                Start = watchEvents[0].WatchedAt,
                End = watchEvents[^1].WatchedAt
            },
            TopCategories = categoryCounts
        };

        return new ServiceResponse<TakeoutSummaryDto>(
            summary,
            $"Processed {summary.TotalVideos} watch events.");
    }

    private static ParsedTakeoutRecord? TryParseRecord(TakeoutRecord record)
    {
        if (!string.Equals(record.Header, "YouTube", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!TryParseWatchedAt(record.Time, out var watchedAtUtc))
        {
            return null;
        }

        var title = NormalizeTitle(record.Title);
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var videoId = ExtractVideoId(record.TitleUrl);
        if (string.IsNullOrWhiteSpace(videoId))
        {
            return null;
        }

        var channelId = ExtractChannelId(record.Subtitles);
        return new ParsedTakeoutRecord(title, videoId, channelId, watchedAtUtc);
    }

    private static ZipArchiveEntry? FindWatchHistoryEntry(ZipArchive archive)
    {
        foreach (var entry in archive.Entries)
        {
            var normalizedPath = NormalizeArchivePath(entry.FullName);
            if (string.Equals(normalizedPath, WatchHistoryPath, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.EndsWith("/watch-history.json", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedPath, "watch-history.json", StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }

    private static string NormalizeArchivePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    private static bool TryParseWatchedAt(string? value, out DateTime watchedAtUtc)
    {
        if (!DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out watchedAtUtc))
        {
            watchedAtUtc = default;
            return false;
        }

        watchedAtUtc = DateTime.SpecifyKind(watchedAtUtc, DateTimeKind.Utc);
        return true;
    }

    private static string NormalizeTitle(string? rawTitle)
    {
        const string watchedPrefix = "Watched ";

        var title = rawTitle?.Trim() ?? string.Empty;
        if (title.StartsWith(watchedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            title = title[watchedPrefix.Length..];
        }

        return title.Trim();
    }

    private static string? ExtractVideoId(string? titleUrl)
    {
        if (string.IsNullOrWhiteSpace(titleUrl) ||
            !Uri.TryCreate(titleUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (uri.Host.EndsWith("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            var shortId = uri.AbsolutePath.Trim('/');
            return string.IsNullOrWhiteSpace(shortId) ? null : shortId;
        }

        var query = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var segment in query)
        {
            var parts = segment.Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            if (string.Equals(parts[0], "v", StringComparison.OrdinalIgnoreCase))
            {
                var videoId = Uri.UnescapeDataString(parts[1]);
                return string.IsNullOrWhiteSpace(videoId) ? null : videoId;
            }
        }

        return null;
    }

    private static string? ExtractChannelId(IReadOnlyList<TakeoutSubtitle>? subtitles)
    {
        var channelUrl = subtitles?.FirstOrDefault()?.Url;
        if (string.IsNullOrWhiteSpace(channelUrl) ||
            !Uri.TryCreate(channelUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (string.Equals(segments[index], "channel", StringComparison.OrdinalIgnoreCase))
            {
                var channelId = segments[index + 1];
                return string.IsNullOrWhiteSpace(channelId) ? null : channelId;
            }
        }

        return null;
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static int GetMondayBasedDayOfWeek(DateTime watchedAtUtc)
    {
        return ((int)watchedAtUtc.DayOfWeek + 6) % 7;
    }

    private static string InferCategory(string title)
    {
        var normalizedTitle = title.ToLowerInvariant();

        foreach (var (category, keywords) in CategoryRules)
        {
            if (keywords.Any(keyword => normalizedTitle.Contains(keyword, StringComparison.Ordinal)))
            {
                return category;
            }
        }

        return "Other";
    }
}
