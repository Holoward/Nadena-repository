using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ReviewRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Review>> GetByDatasetIdAsync(int datasetId)
    {
        return await _dbContext.Reviews
            .Where(r => r.DatasetId == datasetId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Review review)
    {
        await _dbContext.Reviews.AddAsync(review);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<double> GetAverageRatingAsync(int datasetId)
    {
        var reviews = await _dbContext.Reviews
            .Where(r => r.DatasetId == datasetId)
            .ToListAsync();

        return reviews.Any() ? reviews.Average(r => r.Rating) : 0.0;
    }

    public async Task<int> GetReviewCountAsync(int datasetId)
    {
        return await _dbContext.Reviews
            .CountAsync(r => r.DatasetId == datasetId);
    }
}
