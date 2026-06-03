using Domain.Entities;

namespace Application.Interfaces;

public interface IDatasetRepository
{
    Task<Dataset> GetByIdAsync(int id);
    Task<IEnumerable<Dataset>> GetAllAsync();
    Task AddAsync(Dataset dataset);
    Task UpdateAsync(Dataset dataset);
    Task<IEnumerable<Dataset>> GetFlaggedAsync();
    IVolunteerRepository? GetVolunteerRepository();
}
