using Domain.Entities;

namespace Application.Interfaces;

public interface IDataLicenseRepository
{
    Task<DataLicense?> GetByIdAsync(Guid id);
    Task<IEnumerable<DataLicense>> GetByBuyerIdStringAsync(string buyerUserId);
    Task<DataLicense?> GetActiveLicenseByApiKeyIdAsync(Guid apiKeyId);
    Task AddAsync(DataLicense license);
    Task UpdateAsync(DataLicense license);
}
