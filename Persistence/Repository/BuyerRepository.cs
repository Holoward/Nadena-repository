using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class BuyerRepository : MyRepositoryAsync<Buyer>, IBuyerRepository
{
    public BuyerRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Buyer> GetByIdAsync(int id)
    {
        return await DbContext.Buyers.FindAsync(id);
    }

    public async Task<IEnumerable<Buyer>> GetAllAsync()
    {
        return await DbContext.Buyers.ToListAsync();
    }

    public async Task<Buyer> GetByUserIdAsync(string userId)
    {
        return await DbContext.Buyers.FirstOrDefaultAsync(b => b.UserId == userId);
    }

    public async Task AddAsync(Buyer buyer)
    {
        await DbContext.Buyers.AddAsync(buyer);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Buyer buyer)
    {
        DbContext.Entry(buyer).State = EntityState.Modified;
        await DbContext.SaveChangesAsync();
    }
}
