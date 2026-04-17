using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service for GDPR compliance operations
/// </summary>
public interface IGdprService
{
    /// <summary>
    /// Exports all data for a volunteer
    /// </summary>
    Task<VolunteerDataExportDto> ExportVolunteerDataAsync(int volunteerId, string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes all personal data for a volunteer (anonymization)
    /// </summary>
    Task<bool> DeleteVolunteerDataAsync(int volunteerId, string userId, CancellationToken cancellationToken = default);
}
