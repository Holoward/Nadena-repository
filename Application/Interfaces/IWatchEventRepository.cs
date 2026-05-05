using Domain.Entities;

namespace Application.Interfaces;

public interface IWatchEventRepository : IRepositoryAsync<WatchEvent>
{
    Task<List<WatchEvent>> GetByContributorIdAsync(Guid contributorId, CancellationToken cancellationToken = default);

    Task ReplaceForContributorAsync(
        Guid contributorId,
        IReadOnlyCollection<WatchEvent> watchEvents,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of records from the backing table specified by
    /// <paramref name="sourceTable"/> together with the total row count.
    /// Pagination is fully DB-side — no in-memory buffering.
    /// </summary>
    /// <param name="sourceTable">
    /// Canonical source-table name as stored in <c>DataPool.SourceTable</c>
    /// (e.g. "WatchEvents", "SpotifyListeningRecords", "NetflixViewingRecords").
    /// Defaults to "WatchEvents".
    /// </param>
    Task<(List<object> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string sourceTable = "WatchEvents",
        string? category = null,
        CancellationToken cancellationToken = default);
}
