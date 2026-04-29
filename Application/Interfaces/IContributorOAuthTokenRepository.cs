using Domain.Entities;

namespace Application.Interfaces;

public interface IContributorOAuthTokenRepository
{
    Task<ContributorOAuthToken?> GetByContributorIdAsync(string contributorId);
    Task<List<ContributorOAuthToken>> GetAllActiveAsync();
    Task AddAsync(ContributorOAuthToken token);
    Task UpdateAsync(ContributorOAuthToken token);
}
