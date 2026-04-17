using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class WalletRepository : IWalletRepository
{
    private readonly ApplicationDbContext _dbContext;

    public WalletRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Wallet?> GetPlatformWalletAsync()
    {
        return await _dbContext.Wallets.FirstOrDefaultAsync(w => w.OwnerId == "platform");
    }

    public async Task<Wallet?> GetByOwnerAsync(string ownerId)
    {
        return await _dbContext.Wallets.FirstOrDefaultAsync(w => w.OwnerId == ownerId);
    }

    public async Task<Wallet?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Wallets.FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<List<Wallet>> ListAsync()
    {
        return await _dbContext.Wallets.OrderBy(w => w.OwnerType).ThenBy(w => w.OwnerId).ToListAsync();
    }

    public async Task AddAsync(Wallet wallet)
    {
        await _dbContext.Wallets.AddAsync(wallet);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Wallet wallet)
    {
        _dbContext.Wallets.Update(wallet);
        await _dbContext.SaveChangesAsync();
    }
}
