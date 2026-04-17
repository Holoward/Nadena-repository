using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Repository interface for Netflix viewing records
/// </summary>
public interface INetflixRecordRepository : IRepositoryAsync<NetflixViewingRecord>
{
    /// <summary>
    /// Gets all Netflix viewing records for a specific volunteer
    /// </summary>
    Task<List<NetflixViewingRecord>> GetByVolunteerIdAsync(int volunteerId);

    /// <summary>
    /// Gets all anonymized Netflix viewing records for volunteers identified by UserIds (Guids)
    /// </summary>
    Task<List<NetflixViewingRecord>> GetAnonymizedByUserIdsAsync(List<Guid> userIds);

    /// <summary>
    /// Adds multiple Netflix viewing records in bulk
    /// </summary>
    Task AddRangeAsync(List<NetflixViewingRecord> records);

    /// <summary>
    /// Gets the count of Netflix viewing records for a specific volunteer
    /// </summary>
    Task<int> GetCountByVolunteerIdAsync(int volunteerId);
}
