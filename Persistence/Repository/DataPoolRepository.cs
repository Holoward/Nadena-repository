using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class DataPoolRepository : MyRepositoryAsync<DataPool>, IDataPoolRepository
{
    private new ApplicationDbContext DbContext => (ApplicationDbContext)base.DbContext;

    public DataPoolRepository(ApplicationDbContext dbContext) : base(dbContext) { }

    public async Task<DataPool?> GetByIdAsync(int id)
        => await DbContext.DataPools.FindAsync(id);

    public async Task<IEnumerable<DataPool>> GetAllAsync()
        => await DbContext.DataPools.ToListAsync();

    public async Task<IEnumerable<DataPool>> GetAllActiveAsync()
        => await DbContext.DataPools.Where(p => p.IsActive).ToListAsync();

    public async Task AddAsync(DataPool pool)
    {
        await DbContext.DataPools.AddAsync(pool);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(DataPool pool)
    {
        DbContext.DataPools.Update(pool);
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task RecalculateApproximateCountAsync(
        string sourceTable,
        CancellationToken ct = default)
    {
        // Count the live rows in the appropriate backing table.
        long liveCount = sourceTable switch
        {
            "WatchEvents"            => await DbContext.WatchEvents.LongCountAsync(ct),
            "SpotifyListeningRecords" => await DbContext.SpotifyListeningRecords.LongCountAsync(ct),
            "NetflixViewingRecords"  => await DbContext.NetflixViewingRecords.LongCountAsync(ct),
            _                        => 0L
        };

        // Persist to every pool that uses this source table.
        var pools = await DbContext.DataPools
            .Where(p => p.SourceTable == sourceTable)
            .ToListAsync(ct);

        foreach (var pool in pools)
            pool.ApproximateRecordCount = liveCount;

        if (pools.Count > 0)
            await DbContext.SaveChangesAsync(ct);
    }
}
