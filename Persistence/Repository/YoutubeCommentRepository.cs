using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class YoutubeCommentRepository : MyRepositoryAsync<YoutubeComment>, IYoutubeCommentRepository
{
    public YoutubeCommentRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<int> BulkInsertAsync(IEnumerable<YoutubeComment> comments)
    {
        await DbContext.YoutubeComments.AddRangeAsync(comments);
        return await DbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<YoutubeComment>> GetByPoolIdAsync(int poolId, int page, int pageSize)
    {
        // Join through Datasets to find comments in this pool
        var datasetIds = await DbContext.Datasets
            .Where(d => d.DataPoolId == poolId)
            .Select(d => d.Id)
            .ToListAsync();

        var query = DbContext.YoutubeComments
            .Where(c => c.IsAnonymized);

        if (datasetIds.Count > 0)
        {
            query = query.Where(c => datasetIds.Contains(c.VolunteerId)); // Legacy linkage fallback
        }

        return await query
            .OrderByDescending(c => c.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<YoutubeComment>> GetByVolunteerIdAsync(int volunteerId)
    {
        return await DbContext.YoutubeComments
            .Where(c => c.VolunteerId == volunteerId)
            .ToListAsync();
    }
}
