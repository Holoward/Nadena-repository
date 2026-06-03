using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class SpotifyRecordRepository : MyRepositoryAsync<SpotifyListeningRecord>, ISpotifyRecordRepository
{
    private readonly IVolunteerRepository _volunteerRepository;
    private new ApplicationDbContext DbContext => (ApplicationDbContext)base.DbContext;

    public SpotifyRecordRepository(ApplicationDbContext dbContext, IVolunteerRepository volunteerRepository) : base(dbContext)
    {
        _volunteerRepository = volunteerRepository;
    }

    public async Task<List<SpotifyListeningRecord>> GetByVolunteerIdAsync(int volunteerId)
    {
        return await DbContext.SpotifyListeningRecords
            .Where(r => r.VolunteerId == volunteerId)
            .ToListAsync();
    }

    public async Task<List<SpotifyListeningRecord>> GetAnonymizedByUserIdsAsync(List<Guid> userIds)
    {
        var volunteers = await _volunteerRepository.GetByIdsAsync(userIds);
        var volunteerIds = volunteers.Select(v => v.Id).ToList();

        return await DbContext.SpotifyListeningRecords
            .Where(r => volunteerIds.Contains(r.VolunteerId) && r.IsAnonymized)
            .ToListAsync();
    }


    public async Task AddRangeAsync(List<SpotifyListeningRecord> records)
    {
        await DbContext.SpotifyListeningRecords.AddRangeAsync(records);
        await DbContext.SaveChangesAsync();
    }

    public async Task<int> GetCountByVolunteerIdAsync(int volunteerId)
    {
        return await DbContext.SpotifyListeningRecords
            .CountAsync(r => r.VolunteerId == volunteerId);
    }
}
