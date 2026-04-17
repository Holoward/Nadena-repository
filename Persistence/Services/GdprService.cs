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

        // Get all YoutubeComment records
        var youtubeComments = await _context.YoutubeComments
            .Where(c => c.VolunteerId == volunteerId)
            .ToListAsync(cancellationToken);

        // Get all ConsentRecord records
        var consentRecords = await _context.ConsentRecords
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);

        // Get payment history
        var payments = await _context.VolunteerPayments
            .Where(p => p.VolunteerId == volunteerId)
            .ToListAsync(cancellationToken);

        // Get upload history (inferred from comment timestamps)
        var uploadHistory = youtubeComments
            .GroupBy(c => new { c.Timestamp.Year, c.Timestamp.Month })
            .Select(g => new UploadHistoryExportDto
            {
                UploadedAt = new DateTime(g.Key.Year, g.Key.Month, 1),
                CommentCount = g.Count(),
                FileName = $"Comments_{g.Key.Year}_{g.Key.Month:D2}"
            })
            .OrderByDescending(u => u.UploadedAt)
            .ToList();

        // Build the export DTO
        return new VolunteerDataExportDto
        {
            VolunteerId = volunteer.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PayPalEmail = volunteer.PayPalEmail,
            Status = volunteer.Status.ToString(),
            CreatedAt = volunteer.Created,
            YoutubeComments = youtubeComments.Select(c => new YoutubeCommentExportDto
            {
                VideoId = c.VideoId,
                VideoTitle = string.Empty,
                CommentText = c.CommentText,
                CommentDate = c.Timestamp
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
            UploadHistory = uploadHistory
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

        // Delete all YoutubeComment records
        var youtubeComments = await _context.YoutubeComments
            .Where(c => c.VolunteerId == volunteerId)
            .ToListAsync(cancellationToken);
        _context.YoutubeComments.RemoveRange(youtubeComments);

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
            newValues: $"{{\"Status\":\"{VolunteerStatus.Deleted}\",\"DeletedCommentsCount\":{youtubeComments.Count},\"DeletedSpotifyRecordsCount\":{spotifyRecords.Count}}}");

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
