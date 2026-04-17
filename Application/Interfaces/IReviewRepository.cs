using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Repository interface for dataset reviews
/// </summary>
public interface IReviewRepository
{
    /// <summary>
    /// Gets all reviews for a specific dataset
    /// </summary>
    Task<List<Review>> GetByDatasetIdAsync(int datasetId);

    /// <summary>
    /// Adds a new review
    /// </summary>
    Task AddAsync(Review review);

    /// <summary>
    /// Gets the average rating for a dataset
    /// </summary>
    Task<double> GetAverageRatingAsync(int datasetId);

    /// <summary>
    /// Gets the review count for a dataset
    /// </summary>
    Task<int> GetReviewCountAsync(int datasetId);
}
