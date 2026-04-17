using Domain.Entities;

namespace Application.Interfaces;

public interface IDataPoolRepository
{
    Task<DataPool?> GetByIdAsync(int id);
    Task<IEnumerable<DataPool>> GetAllAsync();
    Task<IEnumerable<DataPool>> GetAllActiveAsync();
    Task AddAsync(DataPool pool);
    Task UpdateAsync(DataPool pool);
}
