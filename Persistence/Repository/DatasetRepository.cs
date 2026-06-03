using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class DatasetRepository : MyRepositoryAsync<Dataset>, IDatasetRepository
{
    private readonly IVolunteerRepository _volunteerRepository;
    private new ApplicationDbContext DbContext => (ApplicationDbContext)base.DbContext;

    public DatasetRepository(ApplicationDbContext dbContext, IVolunteerRepository volunteerRepository) : base(dbContext)
    {
        _volunteerRepository = volunteerRepository;
    }

    public async Task<Dataset> GetByIdAsync(int id)
    {
        return await DbContext.Datasets.FindAsync(id);
    }

    public async Task<IEnumerable<Dataset>> GetAllAsync()
    {
        return await DbContext.Datasets.ToListAsync();
    }

    public async Task AddAsync(Dataset dataset)
    {
        await DbContext.Datasets.AddAsync(dataset);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Dataset dataset)
    {
        DbContext.Datasets.Update(dataset);
        await DbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Dataset>> GetFlaggedAsync()
    {
        return await DbContext.Datasets
            .Where(d => d.IntegrityStatus == Domain.Enums.IntegrityStatus.Flagged)
            .ToListAsync();
    }

    public IVolunteerRepository? GetVolunteerRepository() => _volunteerRepository;
}
