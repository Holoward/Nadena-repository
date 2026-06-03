using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public sealed class WatchEventRepository : MyRepositoryAsync<WatchEvent>, IWatchEventRepository
{
    private new ApplicationDbContext DbContext => (ApplicationDbContext)base.DbContext;

    public WatchEventRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<List<WatchEvent>> GetByContributorIdAsync(
        Guid contributorId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.WatchEvents
            .Where(watchEvent => watchEvent.ContributorId == contributorId)
            .AsNoTracking()
            .OrderBy(watchEvent => watchEvent.WatchedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceForContributorAsync(
        Guid contributorId,
        IReadOnlyCollection<WatchEvent> watchEvents,
        CancellationToken cancellationToken = default)
    {
        await DbContext.WatchEvents
            .Where(watchEvent => watchEvent.ContributorId == contributorId)
            .ExecuteDeleteAsync(cancellationToken);

        if (watchEvents.Count > 0)
        {
            await DbContext.WatchEvents.AddRangeAsync(watchEvents, cancellationToken);
        }

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(List<object> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string sourceTable = "WatchEvents",
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);
        var skip = (page - 1) * pageSize;

        switch (sourceTable)
        {
            case "SpotifyListeningRecords":
            {
                var query = DbContext.SpotifyListeningRecords.AsNoTracking();
                var total = await query.CountAsync(cancellationToken);
                var items = await query
                    .OrderBy(r => r.Id)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(r => (object)new
                    {
                        r.Platform,
                        r.PlayedAt,
                        MsPlayed = r.MsPlayed
                    })
                    .ToListAsync(cancellationToken);
                return (items, total);
            }

            case "NetflixViewingRecords":
            {
                var query = DbContext.NetflixViewingRecords.AsNoTracking();
                var total = await query.CountAsync(cancellationToken);
                var items = await query
                    .OrderBy(r => r.Id)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(r => (object)new
                    {
                        r.WatchedDate,
                        r.DurationMinutes,
                        r.DeviceType,
                        r.Country
                    })
                    .ToListAsync(cancellationToken);
                return (items, total);
            }

            // Default: WatchEvents
            default:
            {
                var query = DbContext.WatchEvents.AsNoTracking();
                if (!string.IsNullOrWhiteSpace(category))
                    query = query.Where(w => w.Category == category);

                var total = await query.CountAsync(cancellationToken);
                var items = await query
                    .OrderBy(w => w.Id)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(w => (object)new
                    {
                        w.Category,
                        w.HourOfDay,
                        w.DayOfWeek,
                        w.Month,
                        w.Year,
                        w.SessionId,
                        w.PositionInSession,
                        w.IsRepeat
                    })
                    .ToListAsync(cancellationToken);
                return (items, total);
            }
        }
    }
}
