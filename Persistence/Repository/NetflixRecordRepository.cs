using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class NetflixRecordRepository : MyRepositoryAsync<NetflixViewingRecord>, INetflixRecordRepository
{
    public NetflixRecordRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<List<NetflixViewingRecord>> GetByVolunteerIdAsync(int volunteerId)
    {
        return await DbContext.NetflixViewingRecords
            .Where(r => r.VolunteerId == volunteerId)
            .ToListAsync();
    }

    public async Task<List<NetflixViewingRecord>> GetAnonymizedByUserIdsAsync(List<Guid> userIds)
    {
        var userIdStrings = userIds.Select(g => g.ToString()).ToList();
        
        var volunteerIds = await DbContext.Volunteers
            .Where(v => userIdStrings.Contains(v.UserId))
            .Select(v => v.Id)
            .ToListAsync();

        return await DbContext.NetflixViewingRecords
            .Where(r => volunteerIds.Contains(r.VolunteerId) && r.IsAnonymized)
            .ToListAsync();
    }

    public async Task AddRangeAsync(List<NetflixViewingRecord> records)
    {
        await DbContext.NetflixViewingRecords.AddRangeAsync(records);
        await DbContext.SaveChangesAsync();
    }

    public async Task<int> GetCountByVolunteerIdAsync(int volunteerId)
    {
        return await DbContext.NetflixViewingRecords
            .CountAsync(r => r.VolunteerId == volunteerId);
    }
}
