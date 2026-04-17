using Domain.Entities;

namespace Application.Interfaces;

public interface IYoutubeCommentRepository : IRepositoryAsync<YoutubeComment>
{
    Task<int> BulkInsertAsync(IEnumerable<YoutubeComment> comments);
    Task<IEnumerable<YoutubeComment>> GetByPoolIdAsync(int poolId, int page, int pageSize);
    Task<IEnumerable<YoutubeComment>> GetByVolunteerIdAsync(int volunteerId);
}
