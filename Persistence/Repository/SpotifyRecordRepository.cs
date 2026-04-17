using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class SpotifyRecordRepository : MyRepositoryAsync<SpotifyListeningRecord>, ISpotifyRecordRepository
{
    public SpotifyRecordRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<List<SpotifyListeningRecord>> GetByVolunteerIdAsync(int volunteerId)
    {
        return await DbContext.SpotifyListeningRecords
            .Where(r => r.VolunteerId == volunteerId)
            .ToListAsync();
    }

    public async Task<List<SpotifyListeningRecord>> GetAnonymizedByUserIdsAsync(List<Guid> userIds)
    {
        var userIdStrings = userIds.Select(g => g.ToString()).ToList();
        
        var volunteerIds = await DbContext.Volunteers
            .Where(v => userIdStrings.Contains(v.UserId))
            .Select(v => v.Id)
            .ToListAsync();

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
