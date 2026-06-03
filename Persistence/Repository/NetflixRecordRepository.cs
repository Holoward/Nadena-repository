using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class NetflixRecordRepository : MyRepositoryAsync<NetflixViewingRecord>, INetflixRecordRepository
{
    private readonly IVolunteerRepository _volunteerRepository;
    private new ApplicationDbContext DbContext => (ApplicationDbContext)base.DbContext;

    public NetflixRecordRepository(ApplicationDbContext dbContext, IVolunteerRepository volunteerRepository) : base(dbContext)
    {
        _volunteerRepository = volunteerRepository;
    }

    public async Task<List<NetflixViewingRecord>> GetByVolunteerIdAsync(int volunteerId)
    {
        return await DbContext.NetflixViewingRecords
            .Where(r => r.VolunteerId == volunteerId)
            .ToListAsync();
    }

    public async Task<List<NetflixViewingRecord>> GetAnonymizedByUserIdsAsync(List<Guid> userIds)
    {
        var volunteers = await _volunteerRepository.GetByIdsAsync(userIds);
        var volunteerIds = volunteers.Select(v => v.Id).ToList();

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
