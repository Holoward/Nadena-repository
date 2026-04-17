using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Takeout.Queries;

public sealed class ContributorStatsDto
{
    public int TotalVideos { get; set; }
    public int UniqueVideos { get; set; }
    public int UniqueChannels { get; set; }
    public double RepeatViewRate { get; set; }
    public double AvgVideosPerDay { get; set; }
    public int PeakHour { get; set; }
    public int PeakDay { get; set; }
    public int PeakMonth { get; set; }
    public int SessionCount { get; set; }
    public double AvgSessionLength { get; set; }
    public double BingeRate { get; set; }
    public double DiversityScore { get; set; }
    public double CategoryEntropy { get; set; }
    public int[] HourDistribution { get; set; } = new int[24];
    public int[] DayDistribution { get; set; } = new int[7];
    public Dictionary<string, double> CategoryPercentages { get; set; } = new();
    public DateTime? HistoryStart { get; set; }
    public DateTime? HistoryEnd { get; set; }
    public int HistoryDays { get; set; }
}

public sealed class GetContributorStatsQuery : IRequest<ServiceResponse<ContributorStatsDto>>
{
    public Guid ContributorId { get; init; }
}

public sealed class GetContributorStatsQueryHandler
    : IRequestHandler<GetContributorStatsQuery, ServiceResponse<ContributorStatsDto>>
{
    private static readonly string[] Categories =
    [
        "Tech",
        "Entertainment",
        "News",
        "Gaming",
        "Music",
        "Education",
        "Sports",
        "Cooking",
        "Other"
    ];

    private readonly IWatchEventRepository _watchEventRepository;

    public GetContributorStatsQueryHandler(IWatchEventRepository watchEventRepository)
    {
        _watchEventRepository = watchEventRepository;
    }

    public async Task<ServiceResponse<ContributorStatsDto>> Handle(
        GetContributorStatsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ContributorId == Guid.Empty)
        {
            return new ServiceResponse<ContributorStatsDto>("A valid contributorId is required.");
        }

        var events = await _watchEventRepository.GetByContributorIdAsync(
            request.ContributorId,
            cancellationToken);

        if (events.Count == 0)
        {
            return new ServiceResponse<ContributorStatsDto>("No watch events found for this contributor.");
        }

        var totalVideos = events.Count;
        var uniqueVideos = events
            .Select(watchEvent => watchEvent.VideoIdHash)
            .Where(videoIdHash => !string.IsNullOrWhiteSpace(videoIdHash))
            .Distinct(StringComparer.Ordinal)
            .Count();

        var uniqueChannels = events
            .Select(watchEvent => watchEvent.ChannelIdHash)
            .Where(channelIdHash => !string.IsNullOrWhiteSpace(channelIdHash))
            .Distinct(StringComparer.Ordinal)
            .Count();

        var repeatViewRate = Math.Round((double)events.Count(watchEvent => watchEvent.IsRepeat) / totalVideos, 4);

        var historyStart = events[0].WatchedAt;
        var historyEnd = events[^1].WatchedAt;
        var historyDays = (historyEnd.Date - historyStart.Date).Days + 1;
        var avgVideosPerDay = Math.Round((double)totalVideos / historyDays, 2);

        var hourDistribution = new int[24];
        var dayDistribution = new int[7];

        foreach (var watchEvent in events)
        {
            if ((uint)watchEvent.HourOfDay < hourDistribution.Length)
            {
                hourDistribution[watchEvent.HourOfDay]++;
            }

            if ((uint)watchEvent.DayOfWeek < dayDistribution.Length)
            {
                dayDistribution[watchEvent.DayOfWeek]++;
            }
        }

        var peakHour = GetPeakIndex(hourDistribution);
        var peakDay = GetPeakIndex(dayDistribution);
        var peakMonth = events
            .GroupBy(watchEvent => watchEvent.Month)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .First();

        var sessionGroups = events
            .GroupBy(watchEvent => watchEvent.SessionId)
            .OrderBy(group => group.Key)
            .ToList();

        var sessionCount = sessionGroups.Count;
        var avgSessionLength = Math.Round(sessionGroups.Average(group => group.Count()), 2);
        var bingeRate = Math.Round((double)sessionGroups.Count(group => group.Count() >= 3) / sessionCount, 4);

        var categoryCounts = Categories.ToDictionary(category => category, _ => 0, StringComparer.Ordinal);
        foreach (var watchEvent in events)
        {
            if (categoryCounts.ContainsKey(watchEvent.Category))
            {
                categoryCounts[watchEvent.Category]++;
            }
            else
            {
                categoryCounts["Other"]++;
            }
        }

        var categoryPercentages = new Dictionary<string, double>(Categories.Length, StringComparer.Ordinal);
        var categoryEntropy = 0d;

        foreach (var category in Categories)
        {
            var count = categoryCounts[category];
            var proportion = (double)count / totalVideos;

            categoryPercentages[category] = Math.Round(proportion * 100, 2);

            if (proportion > 0)
            {
                categoryEntropy -= proportion * Math.Log2(proportion);
            }
        }

        categoryEntropy = Math.Round(categoryEntropy, 4);

        // Uses unique channels as a simple diversity proxy: closer to 1 means broader viewing spread.
        var diversityScore = Math.Round((double)uniqueChannels / totalVideos, 4);

        var response = new ContributorStatsDto
        {
            TotalVideos = totalVideos,
            UniqueVideos = uniqueVideos,
            UniqueChannels = uniqueChannels,
            RepeatViewRate = repeatViewRate,
            AvgVideosPerDay = avgVideosPerDay,
            PeakHour = peakHour,
            PeakDay = peakDay,
            PeakMonth = peakMonth,
            SessionCount = sessionCount,
            AvgSessionLength = avgSessionLength,
            BingeRate = bingeRate,
            DiversityScore = diversityScore,
            CategoryEntropy = categoryEntropy,
            HourDistribution = hourDistribution,
            DayDistribution = dayDistribution,
            CategoryPercentages = categoryPercentages,
            HistoryStart = historyStart,
            HistoryEnd = historyEnd,
            HistoryDays = historyDays
        };

        return new ServiceResponse<ContributorStatsDto>(
            response,
            $"Computed contributor stats from {totalVideos} watch events.");
    }

    private static int GetPeakIndex(IReadOnlyList<int> values)
    {
        var bestIndex = 0;
        var bestValue = int.MinValue;

        for (var index = 0; index < values.Count; index++)
        {
            if (values[index] > bestValue)
            {
                bestIndex = index;
                bestValue = values[index];
            }
        }

        return bestIndex;
    }
}
