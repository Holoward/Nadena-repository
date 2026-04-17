using Domain.Entities;

namespace Application.Interfaces;

public interface IDatasetRepository
{
    Task<Dataset> GetByIdAsync(int id);
    Task<IEnumerable<Dataset>> GetAllAsync();
    Task AddAsync(Dataset dataset);
    Task UpdateAsync(Dataset dataset);
    Task<List<YoutubeComment>> GetAnonymizedCommentsByVolunteerIds(List<Guid> volunteerIds);
    Task<IEnumerable<Dataset>> GetFlaggedAsync();
    IVolunteerRepository? GetVolunteerRepository();
}
