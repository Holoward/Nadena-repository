using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class ConsentRecordRepository : MyRepositoryAsync<ConsentRecord>, IConsentRecordRepository
{
    public ConsentRecordRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task AddAsync(ConsentRecord consentRecord)
    {
        await DbContext.ConsentRecords.AddAsync(consentRecord);
        await DbContext.SaveChangesAsync();
    }

    public async Task<ConsentRecord?> GetLatestByUserIdAsync(string userId)
    {
        return await DbContext.ConsentRecords
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.AgreedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<ConsentRecord?> GetLatestByUserIdAndDocumentAsync(string userId, string documentType)
    {
        return await DbContext.ConsentRecords
            .Where(c => c.UserId == userId && c.DocumentType == documentType)
            .OrderByDescending(c => c.AgreedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasAcceptedAsync(string userId, string documentType)
    {
        return await DbContext.ConsentRecords.AnyAsync(c => c.UserId == userId && c.DocumentType == documentType);
    }
}
