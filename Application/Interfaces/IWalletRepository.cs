using Domain.Entities;

namespace Application.Interfaces;

public interface IWalletRepository
{
    Task<Wallet?> GetPlatformWalletAsync();
    Task<Wallet?> GetByOwnerAsync(string ownerId);
    Task<Wallet?> GetByIdAsync(Guid id);
    Task<List<Wallet>> ListAsync();
    Task AddAsync(Wallet wallet);
    Task UpdateAsync(Wallet wallet);
}
