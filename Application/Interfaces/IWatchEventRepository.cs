using Domain.Entities;

namespace Application.Interfaces;

public interface IWatchEventRepository : IRepositoryAsync<WatchEvent>
{
    Task<List<WatchEvent>> GetByContributorIdAsync(Guid contributorId, CancellationToken cancellationToken = default);
    Task ReplaceForContributorAsync(
        Guid contributorId,
        IReadOnlyCollection<WatchEvent> watchEvents,
        CancellationToken cancellationToken = default);
}
