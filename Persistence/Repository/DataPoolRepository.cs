using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class DataPoolRepository : MyRepositoryAsync<DataPool>, IDataPoolRepository
{
    public DataPoolRepository(ApplicationDbContext dbContext) : base(dbContext) { }

    public async Task<DataPool?> GetByIdAsync(int id)
        => await DbContext.DataPools.FindAsync(id);

    public async Task<IEnumerable<DataPool>> GetAllAsync()
        => await DbContext.DataPools.ToListAsync();

    public async Task<IEnumerable<DataPool>> GetAllActiveAsync()
        => await DbContext.DataPools.Where(p => p.IsActive).ToListAsync();

    public async Task AddAsync(DataPool pool)
    {
        await DbContext.DataPools.AddAsync(pool);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(DataPool pool)
    {
        DbContext.DataPools.Update(pool);
        await DbContext.SaveChangesAsync();
    }
}
