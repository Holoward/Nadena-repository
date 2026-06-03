using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Models;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public class AccountController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly NadenaIdentityDbContext _identityContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;

    public AccountController(
        ApplicationDbContext dbContext,
        NadenaIdentityDbContext identityContext,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _userManager = userManager;
        _emailService = emailService;
        _auditLogService = auditLogService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var preferences = await _identityContext.NotificationPreferences.AsNoTracking()
            .Where(preference => preference.UserId == userId)
            .ToListAsync();

        return Ok(new
        {
            data = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.CompanyName,
                user.CompanyVerified,
                user.EmailConfirmed,
                user.IsSuspended,
                user.DeletedAt,
                notificationPreferences = preferences
            }
        });
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateAccountSettingsRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.FullName = request.FullName?.Trim() ?? user.FullName;
        bool emailChanged = false;
        if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (!Application.Common.InputSanitizer.IsValidEmail(request.Email.Trim()))
                return BadRequest(new { message = "Invalid email format." });
            user.Email = request.Email.Trim();
            user.UserName = request.Email.Trim();
            user.EmailConfirmed = false;
            emailChanged = true;
        }

        if (!string.IsNullOrWhiteSpace(request.CompanyName))
        {
            user.CompanyName = request.CompanyName.Trim();
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(new { message = string.Join(", ", updateResult.Errors.Select(error => error.Description)) });
        }

        if (emailChanged)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var safeEmail = user.Email ?? string.Empty;
            var link = $"https://nadena.com/verify-email?email={Uri.EscapeDataString(safeEmail)}&token={Uri.EscapeDataString(token)}";
            await _emailService.SendEmailVerificationAsync(safeEmail, link);
        }

        await _auditLogService.LogAsync("AccountUpdated", "User", user.Id, true, user.Id);
        return Ok(new { message = "Account settings updated. If you changed your email, please verify it." });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(error => error.Description)) });
        }

        user.LastPasswordChangedAt = DateTime.UtcNow;
        user.SecurityStamp = Guid.NewGuid().ToString();
        await _userManager.UpdateAsync(user);
        await _auditLogService.LogAsync("PasswordChanged", "User", user.Id, true, user.Id);
        return Ok(new { message = "Password changed successfully." });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.DeletedAt = DateTime.UtcNow.AddDays(30);
        await _userManager.UpdateAsync(user);
        await _emailService.SendAccountDeletionConfirmationAsync(user.Email ?? user.UserName ?? string.Empty, user.FullName);
        await _auditLogService.LogAsync("AccountDeletionScheduled", "User", user.Id, true, user.Id);
        return Ok(new { message = "Account deletion scheduled. Data will be retained for 30 days." });
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> Sessions()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var sessions = await _identityContext.UserSessions.AsNoTracking()
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.Created)
            .ToListAsync();

        return Ok(new { data = sessions });
    }

    [HttpPost("sessions/logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var sessions = await _identityContext.UserSessions.Where(session => session.UserId == userId && session.IsActive).ToListAsync();
        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.RevokedAt = DateTime.UtcNow;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.SecurityStamp = Guid.NewGuid().ToString();
            await _userManager.UpdateAsync(user);
        }

        await _identityContext.SaveChangesAsync();
        await _auditLogService.LogAsync("AllSessionsRevoked", "UserSession", userId, true, userId);
        return Ok(new { message = "All sessions have been revoked." });
    }

    [HttpGet("notification-preferences")]
    public async Task<IActionResult> NotificationPreferences()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var defaults = NotificationDefaults();
        var stored = await _identityContext.NotificationPreferences
            .Where(preference => preference.UserId == userId)
            .ToListAsync();

        var result = defaults.Select(eventType =>
        {
            var existing = stored.FirstOrDefault(preference => preference.EventType == eventType);
            return new
            {
                eventType,
                isEnabled = existing?.IsEnabled ?? true
            };
        });

        return Ok(new { data = result });
    }

    [HttpPut("notification-preferences")]
    public async Task<IActionResult> UpdateNotificationPreferences([FromBody] List<NotificationPreferenceRequest> request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var existing = await _identityContext.NotificationPreferences.Where(preference => preference.UserId == userId).ToListAsync();
        foreach (var item in request)
        {
            var preference = existing.FirstOrDefault(entry => entry.EventType == item.EventType);
            if (preference == null)
            {
                _identityContext.NotificationPreferences.Add(new Domain.Entities.NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventType = item.EventType,
                    IsEnabled = item.IsEnabled
                });
            }
            else
            {
                preference.IsEnabled = item.IsEnabled;
            }
        }

        await _identityContext.SaveChangesAsync();
        await _auditLogService.LogAsync("NotificationPreferencesUpdated", "NotificationPreference", userId, true, userId);
        return Ok(new { message = "Notification preferences updated." });
    }

    private static List<string> NotificationDefaults()
        => new()
        {
            "DataPurchased",
            "PayoutProcessed",
            "PurchaseConfirmed",
            "RecurringDatasetRefreshed",
            "PasswordReset",
            "EmailVerification",
            "AccountDeletion"
        };
}

public class UpdateAccountSettingsRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? CompanyName { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class NotificationPreferenceRequest
{
    public string EventType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
