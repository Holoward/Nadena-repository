using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public sealed class WatchEventRepository : MyRepositoryAsync<WatchEvent>, IWatchEventRepository
{
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
}
