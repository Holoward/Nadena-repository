using Domain.Entities;

namespace Application.Interfaces;

public interface IDonationRepository
{
    Task AddAsync(Donation donation, CancellationToken cancellationToken = default);
    Task<List<Donation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Donation?> GetByContributorIdAsync(string contributorId, CancellationToken cancellationToken = default);
}