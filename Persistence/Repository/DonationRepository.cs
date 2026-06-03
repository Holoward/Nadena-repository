using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class DonationRepository : IDonationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DonationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Donation donation, CancellationToken cancellationToken = default)
    {
        await _dbContext.Donations.AddAsync(donation, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Donation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Donations
            .AsNoTracking()
            .OrderByDescending(d => d.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Donation?> GetByContributorIdAsync(string contributorId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Donations
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ContributorId == contributorId, cancellationToken);
    }
}