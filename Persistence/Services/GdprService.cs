using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Models;

namespace Persistence.Services;

public class GdprService : IGdprService
{
    private readonly ApplicationDbContext _behavioralContext;
    private readonly NadenaIdentityDbContext _identityContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;

    public GdprService(
        ApplicationDbContext behavioralContext,
        NadenaIdentityDbContext identityContext,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLogService)
    {
        _behavioralContext = behavioralContext;
        _identityContext = identityContext;
        _userManager = userManager;
        _auditLogService = auditLogService;
    }

    public async Task<VolunteerDataExportDto> ExportVolunteerDataAsync(int volunteerId, string userId, CancellationToken cancellationToken = default)
    {
        // Verify ownership
        var volunteer = await _identityContext.Volunteers.FindAsync(new object[] { volunteerId }, cancellationToken);
        if (volunteer == null || volunteer.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only export your own data");
        }

        // Get user details
        var user = await _userManager.FindByIdAsync(volunteer.UserId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Get all ConsentRecord records
        var consentRecords = await _identityContext.ConsentRecords
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);

        // Get payment history
        var payments = await _identityContext.VolunteerPayments
            .Where(p => p.VolunteerId == volunteerId)
            .ToListAsync(cancellationToken);

        var contributorGuid = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty;
        var watchEvents = await _behavioralContext.WatchEvents
            .Where(w => w.ContributorId == contributorGuid)
            .AsNoTracking()
            .OrderBy(w => w.WatchedAt)
            .ToListAsync(cancellationToken);

        // Build the export DTO
        return new VolunteerDataExportDto
        {
            VolunteerId = volunteer.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PayPalEmail = volunteer.PayPalEmail,
            Status = volunteer.Status.ToString(),
            CreatedAt = volunteer.Created,
            WatchEvents = watchEvents.Select(w => new WatchEventExportDto
            {
                Category = w.Category,
                WatchedAt = w.WatchedAt,
                HourOfDay = w.HourOfDay,
                DayOfWeek = w.DayOfWeek,
                Month = w.Month,
                Year = w.Year,
                SessionId = w.SessionId,
                PositionInSession = w.PositionInSession,
                IsRepeat = w.IsRepeat
            }).ToList(),
            ConsentRecords = consentRecords.Select(c => new ConsentRecordExportDto
            {
                ConsentType = c.ConsentText,
                Granted = true,
                GrantedAt = c.AgreedAt,
                ExpiresAt = null
            }).ToList(),
            PaymentHistory = payments.Select(p => new PaymentExportDto
            {
                Amount = p.NetAmount,
                Status = p.Status,
                PaidAt = p.PaidAt ?? DateTime.MinValue,
                TransactionId = p.PayPalPayoutItemId
            }).ToList(),
            UploadHistory = new List<UploadHistoryExportDto>()
        };
    }

    public async Task<bool> DeleteVolunteerDataAsync(int volunteerId, string userId, CancellationToken cancellationToken = default)
    {
        // Verify ownership
        var volunteer = await _identityContext.Volunteers.FindAsync(new object[] { volunteerId }, cancellationToken);
        if (volunteer == null || volunteer.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only delete your own data");
        }

        // Delete all SpotifyListeningRecord records
        var spotifyRecords = await _behavioralContext.SpotifyListeningRecords
            .Where(s => s.VolunteerId == volunteerId)
            .ToListAsync(cancellationToken);
        _behavioralContext.SpotifyListeningRecords.RemoveRange(spotifyRecords);

        var contributorGuid = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty;
        var watchEvents = await _behavioralContext.WatchEvents
            .Where(w => w.ContributorId == contributorGuid)
            .ToListAsync(cancellationToken);
        _behavioralContext.WatchEvents.RemoveRange(watchEvents);

        // Anonymize the Volunteer record
        volunteer.Status = VolunteerStatus.Deleted;

        // Log to AuditLog
        await _auditLogService.LogAsync(
            action: "DataDeletionRequested",
            entityType: "Volunteer",
            entityId: volunteerId.ToString(),
            success: true,
            userId: userId,
            newValues: $"{{\"Status\":\"{VolunteerStatus.Deleted}\",\"DeletedSpotifyRecordsCount\":{spotifyRecords.Count},\"DeletedWatchEventsCount\":{watchEvents.Count}}}");

        await _identityContext.SaveChangesAsync(cancellationToken);
        await _behavioralContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
