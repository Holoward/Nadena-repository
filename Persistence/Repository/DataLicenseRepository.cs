using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class DataLicenseRepository : MyRepositoryAsync<DataLicense>, IDataLicenseRepository
{
    public DataLicenseRepository(ApplicationDbContext dbContext) : base(dbContext) { }

    public async Task<DataLicense?> GetByIdAsync(Guid id)
        => await DbContext.DataLicenses.FindAsync(id);

    public async Task<IEnumerable<DataLicense>> GetByBuyerIdStringAsync(string buyerUserId)
        => await DbContext.DataLicenses
            .Where(l => l.BuyerId == buyerUserId)
            .OrderByDescending(l => l.LicensedFrom)
            .ToListAsync();

    public async Task<DataLicense?> GetActiveLicenseByApiKeyIdAsync(Guid apiKeyId)
        => await DbContext.DataLicenses
            .FirstOrDefaultAsync(l =>
                l.ApiKeyId == apiKeyId &&
                l.Status == LicenseStatus.Active &&
                l.LicensedUntil > DateTime.UtcNow);

    public async Task AddAsync(DataLicense license)
    {
        await DbContext.DataLicenses.AddAsync(license);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(DataLicense license)
    {
        DbContext.DataLicenses.Update(license);
        await DbContext.SaveChangesAsync();
    }
}
