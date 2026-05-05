using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class ContributorOAuthTokenRepository : IContributorOAuthTokenRepository
{
    private readonly NadenaIdentityDbContext _context;

    public ContributorOAuthTokenRepository(NadenaIdentityDbContext context)
    {
        _context = context;
    }

    public async Task<ContributorOAuthToken?> GetByContributorIdAsync(string contributorId)
        => await _context.ContributorOAuthTokens
            .FirstOrDefaultAsync(t => t.ContributorId == contributorId && t.IsActive);

    public async Task<List<ContributorOAuthToken>> GetAllActiveAsync()
        => await _context.ContributorOAuthTokens
            .Where(t => t.IsActive && t.EncryptedRefreshToken != string.Empty)
            .ToListAsync();

    public async Task AddAsync(ContributorOAuthToken token)
    {
        await _context.ContributorOAuthTokens.AddAsync(token);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ContributorOAuthToken token)
    {
        _context.ContributorOAuthTokens.Update(token);
        await _context.SaveChangesAsync();
    }
}
