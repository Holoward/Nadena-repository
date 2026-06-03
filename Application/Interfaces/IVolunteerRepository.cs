using Domain.Entities;

namespace Application.Interfaces;

public interface IVolunteerRepository
{
    Task<Volunteer> GetByIdAsync(int id);
    Task<IEnumerable<Volunteer>> GetAllAsync();
    Task<Volunteer> GetByUserIdAsync(string userId);
    Task AddAsync(Volunteer volunteer);
    Task UpdateAsync(Volunteer volunteer);
    Task<List<Volunteer>> GetVolunteersByIds(List<int> ids);
    Task<(string Email, string FullName)?> GetUserInfoByVolunteerIdAsync(int volunteerId);
    Task<IEnumerable<Volunteer>> GetByIdsAsync(IEnumerable<Guid> userIds);
}
