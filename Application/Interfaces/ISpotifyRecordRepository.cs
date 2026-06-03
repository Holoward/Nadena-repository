using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Repository interface for Spotify listening records
/// </summary>
public interface ISpotifyRecordRepository : IRepositoryAsync<SpotifyListeningRecord>
{
    /// <summary>
    /// Gets all Spotify listening records for a specific volunteer
    /// </summary>
    Task<List<SpotifyListeningRecord>> GetByVolunteerIdAsync(int volunteerId);

    /// <summary>
    /// Gets all anonymized Spotify listening records for volunteers identified by UserIds (Guids)
    /// </summary>
    Task<List<SpotifyListeningRecord>> GetAnonymizedByUserIdsAsync(List<Guid> userIds);

    /// <summary>
    /// Adds multiple Spotify listening records in bulk
    /// </summary>
    Task AddRangeAsync(List<SpotifyListeningRecord> records);

    /// <summary>
    /// Gets the count of Spotify listening records for a specific volunteer
    /// </summary>
    Task<int> GetCountByVolunteerIdAsync(int volunteerId);
}
