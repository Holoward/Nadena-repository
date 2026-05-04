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
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;

    public GdprService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLogService)
    {
        _context = context;
        _userManager = userManager;
        _auditLogService = auditLogService;
    }

    public async Task<VolunteerDataExportDto> ExportVolunteerDataAsync(int volunteerId, string userId, CancellationToken cancellationToken = default)
    {
        // Verify ownership
        var volunteer = await _context.Volunteers.FindAsync(new object[] { volunteerId }, cancellationToken);
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
        var consentRecords = await _context.ConsentRecords
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);

        // Get payment history
        var payments = await _context.VolunteerPayments
            .Where(p => p.VolunteerId == volunteerId)
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
            YoutubeComments = new List<YoutubeCommentExportDto>(),
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
        var volunteer = await _context.Volunteers.FindAsync(new object[] { volunteerId }, cancellationToken);
        if (volunteer == null || volunteer.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only delete your own data");
        }

        // Delete all SpotifyListeningRecord records
        var spotifyRecords = await _context.SpotifyListeningRecords
            .Where(s => s.VolunteerId == volunteerId)
            .ToListAsync(cancellationToken);
        _context.SpotifyListeningRecords.RemoveRange(spotifyRecords);

        // Anonymize the Volunteer record
        volunteer.Status = VolunteerStatus.Deleted;

        // Log to AuditLog
        await _auditLogService.LogAsync(
            action: "DataDeletionRequested",
            entityType: "Volunteer",
            entityId: volunteerId.ToString(),
            success: true,
            userId: userId,
            newValues: $"{{\"Status\":\"{VolunteerStatus.Deleted}\",\"DeletedSpotifyRecordsCount\":{spotifyRecords.Count}}}");

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
