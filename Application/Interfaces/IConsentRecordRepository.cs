using Domain.Entities;

namespace Application.Interfaces;

public interface IConsentRecordRepository
{
    Task AddAsync(ConsentRecord consentRecord);
    Task<ConsentRecord?> GetLatestByUserIdAsync(string userId);
    Task<ConsentRecord?> GetLatestByUserIdAndDocumentAsync(string userId, string documentType);
    Task<bool> HasAcceptedAsync(string userId, string documentType);
}
