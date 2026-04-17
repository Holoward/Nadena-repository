using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class LedgerTransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public LedgerTransactionRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(LedgerTransaction tx)
    {
        await _dbContext.Transactions.AddAsync(tx);
    }

    public async Task<IEnumerable<LedgerTransaction>> GetByWalletAsync(Guid walletId)
    {
        return await _dbContext.Transactions
            .Where(t => t.FromWalletId == walletId || t.ToWalletId == walletId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<LedgerTransaction?> GetByIdempotencyAsync(string idempotencyKey)
    {
        return await _dbContext.Transactions.FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);
    }

    public async Task<LedgerTransaction?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<LedgerTransaction>> ListAsync()
    {
        return await _dbContext.Transactions.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task SaveAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
