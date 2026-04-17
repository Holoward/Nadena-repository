using Domain.Entities;

namespace Application.Interfaces;

public interface IBuyerRepository
{
    Task<Buyer> GetByIdAsync(int id);
    Task<IEnumerable<Buyer>> GetAllAsync();
    Task<Buyer> GetByUserIdAsync(string userId);
    Task AddAsync(Buyer buyer);
    Task UpdateAsync(Buyer buyer);
}
