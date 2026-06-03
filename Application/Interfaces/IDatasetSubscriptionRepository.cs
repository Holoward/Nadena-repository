using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Repository interface for dataset subscriptions
/// </summary>
public interface IDatasetSubscriptionRepository
{
    /// <summary>
    /// Gets all active subscriptions for a specific dataset
    /// </summary>
    Task<List<DatasetSubscription>> GetActiveByDatasetIdAsync(int datasetId);

    /// <summary>
    /// Gets all active subscriptions
    /// </summary>
    Task<List<DatasetSubscription>> GetAllActiveAsync();

    /// <summary>
    /// Adds a new subscription
    /// </summary>
    Task AddAsync(DatasetSubscription subscription);

    /// <summary>
    /// Updates an existing subscription
    /// </summary>
    Task UpdateAsync(DatasetSubscription subscription);
}
