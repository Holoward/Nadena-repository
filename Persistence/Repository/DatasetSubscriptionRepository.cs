using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class DatasetSubscriptionRepository : IDatasetSubscriptionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DatasetSubscriptionRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<DatasetSubscription>> GetActiveByDatasetIdAsync(int datasetId)
    {
        var datasetGuid = new Guid(datasetId.ToString("D"));
        return await _dbContext.DatasetSubscriptions
            .Where(s => s.DatasetId == datasetGuid && s.IsActive)
            .ToListAsync();
    }

    public async Task<List<DatasetSubscription>> GetAllActiveAsync()
    {
        return await _dbContext.DatasetSubscriptions
            .Where(s => s.IsActive)
            .ToListAsync();
    }

    public async Task AddAsync(DatasetSubscription subscription)
    {
        await _dbContext.DatasetSubscriptions.AddAsync(subscription);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(DatasetSubscription subscription)
    {
        _dbContext.DatasetSubscriptions.Update(subscription);
        await _dbContext.SaveChangesAsync();
    }
}
