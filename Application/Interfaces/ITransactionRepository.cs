using Domain.Entities;

namespace Application.Interfaces;

public interface ITransactionRepository
{
    Task AddAsync(LedgerTransaction tx);
    Task<IEnumerable<LedgerTransaction>> GetByWalletAsync(Guid walletId);
    Task<LedgerTransaction?> GetByIdempotencyAsync(string idempotencyKey);
    Task<LedgerTransaction?> GetByIdAsync(Guid id);
    Task<List<LedgerTransaction>> ListAsync();
    Task SaveAsync();
}
