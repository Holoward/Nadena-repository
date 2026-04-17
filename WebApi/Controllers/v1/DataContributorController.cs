using Application.Common;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Persistence.Models;
using Application.Features.Volunteers.Commands.EstimateDataValue;
using Application.Features.Volunteers.Commands.CreateVolunteer;
using Application.Features.Volunteers.Commands.ProcessUploadedFile;
using Application.Features.Volunteers.Commands.RequestDataDeletion;
using Application.Features.Volunteers.Commands.SetupVolunteerData;
using Application.Features.Volunteers.Commands.UpdateVolunteerStatus;
using Application.Features.Volunteers.Queries.ExportMyData;
using Application.Features.Volunteers.Queries.GetAllVolunteers;
using Application.Features.Volunteers.Queries.GetVolunteerById;
using Application.Features.Volunteers.Queries.GetVolunteerByUserId;
using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using System.Text.Json;
using WebApi.Filters;
using System.Text;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DataContributorController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IAuditLogService _auditLogService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DataContributorController(
        ISender mediator, 
        ApplicationDbContext context, 
        IPaymentService paymentService, 
        IAuditLogService auditLogService,
        UserManager<ApplicationUser> userManager)
    {
        _mediator = mediator;
        _context = context;
        _paymentService = paymentService;
        _auditLogService = auditLogService;
        _userManager = userManager;
    }

    // GET: api/v1/DataContributor/me
    [HttpGet("me")]
    [Authorize(Roles = "Data Contributor")]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var volunteer = await _context.Volunteers.AsNoTracking().FirstOrDefaultAsync(v => v.UserId == userId);
        if (volunteer == null)
        {
            return NotFound(new { message = "Data Contributor not found" });
        }

        return Ok(new { data = volunteer });
    }

    // PUT: api/v1/DataContributor/data-sources
    [HttpPut("data-sources")]
    [Authorize(Roles = "Data Contributor")]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> UpdateDataSources([FromBody] DataContributorDataSourcesRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.UserId == userId);
        if (volunteer == null)
        {
            return NotFound(new { message = "Data Contributor not found" });
        }

        var enabledSources = new List<string>();
        if (request.YouTube?.Enabled == true) enabledSources.Add("YouTube");
        if (request.Spotify?.Enabled == true) enabledSources.Add("Spotify");
        if (request.Netflix?.Enabled == true) enabledSources.Add("Netflix");

        volunteer.ContentTypes = string.Join(", ", enabledSources);

        var existing = NotesJson.TryParseObject(volunteer.Notes);
        existing["dataSources"] = new Dictionary<string, object?>
        {
            ["YouTube"] = request.YouTube,
            ["Spotify"] = request.Spotify,
            ["Netflix"] = request.Netflix
        };

        volunteer.Notes = JsonSerializer.Serialize(existing);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Data sources updated.", data = volunteer });
    }

    // GET: api/v1/DataContributor/upload-history
    [HttpGet("upload-history")]
    [Authorize(Roles = "Data Contributor")]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> UploadHistory([FromQuery] int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 200);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var uploads = await _context.AuditLogs.AsNoTracking()
            .Where(a => a.UserId == userId && a.Action == "FileUploaded" && a.Success)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => new
            {
                a.Timestamp,
                a.EntityId,
                a.NewValues
            })
            .ToListAsync();

        return Ok(new { data = uploads });
    }

    // GET: api/v1/DataContributor/earnings
    [HttpGet("earnings")]
    [Authorize(Roles = "Data Contributor")]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> Earnings()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var volunteer = await _context.Volunteers.AsNoTracking().FirstOrDefaultAsync(v => v.UserId == userId);
        if (volunteer == null)
        {
            return NotFound(new { message = "Data Contributor not found" });
        }

        var payments = await _context.VolunteerPayments.AsNoTracking()
            .Where(p => p.VolunteerId == volunteer.Id)
            .OrderByDescending(p => p.PaidAt ?? p.Created)
            .Take(100)
            .ToListAsync();

        var completed = payments.Where(p => string.Equals(p.Status, "Completed", StringComparison.OrdinalIgnoreCase) || p.PaidAt != null);
        var pending = payments.Where(p => string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase));

        return Ok(new
        {
            totals = new
            {
                lifetimeEarnings = completed.Sum(p => p.NetAmount),
                pendingPayments = pending.Sum(p => p.NetAmount)
            },
            payments = payments.Select(p => new
            {
                p.Id,
                p.DatasetId,
                p.NetAmount,
                p.Status,
                p.PaidAt,
                p.Created
            })
        });
    }

    [HttpGet("wallet")]
    [Authorize(Roles = "Data Contributor")]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> Wallet()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var wallet = await _context.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.OwnerId == userId);
        if (wallet == null)
        {
            return Ok(new { data = new { balance = 0m, pendingBalance = 0m, currency = "USD" } });
        }

        return Ok(new { data = wallet });
    }

    [HttpGet("transactions")]
    [Authorize(Roles = "Data Contributor")]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> Transactions()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var wallet = await _context.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.OwnerId == userId);
        if (wallet == null)
        {
            return Ok(new { data = Array.Empty<object>() });
        }

        var transactions = await _context.Transactions.AsNoTracking()
            .Where(t => t.FromWalletId == wallet.Id || t.ToWalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(new { data = transactions });
    }

    [HttpPost("payouts/{transactionId:guid}/approve")]
    [Authorize(Roles = "Data Contributor")]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> ApproveHeldPayout(Guid transactionId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var wallet = await _context.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.OwnerId == userId);
        var transaction = wallet == null
            ? null
            : await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == transactionId && t.ToWalletId == wallet.Id);

        if (transaction == null)
        {
            return Forbid();
        }

        var result = await _paymentService.ReleaseHeldPayoutAsync(transactionId, userId);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message, transactionId = result.Data });
    }

    // GET: api/v1/DataContributor
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Get([FromQuery] int page = 1, int pageSize = 20)
    {
        var paginationParams = new PaginationParams { Page = page, PageSize = pageSize };
        return Ok(await _mediator.Send(new GetAllVolunteersQuery { PaginationParams = paginationParams }));
    }

    // GET: api/v1/DataContributor/5
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Get(int id)
        => Ok(await _mediator.Send(new GetVolunteerByIdQuery { Id = id }));

    // GET: api/v1/DataContributor/user/{userId}
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin,Data Contributor")]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> GetByUserId(string userId)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!User.IsInRole("Admin") && User.IsInRole("Data Contributor") && !string.Equals(currentUserId, userId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        return Ok(await _mediator.Send(new GetVolunteerByUserIdQuery { UserId = userId }));
    }

    // POST: api/v1/DataContributor/setup
    [HttpPost("setup")]
    [Authorize(Roles = "Data Contributor")]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> SetupData([FromBody] SetupVolunteerDataCommand command)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        command.UserId = userId;
        return Ok(await _mediator.Send(command));
    }

    // POST: api/v1/DataContributor (admin create)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Post(CreateVolunteerCommand command)
        => Ok(await _mediator.Send(command));

    // PUT: api/v1/DataContributor/{id}/status (admin)
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateVolunteerStatusCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest();
        }

        return Ok(await _mediator.Send(command));
    }

    // POST: api/v1/DataContributor/{id}/upload
    [HttpPost("{id}/upload")]
    [Authorize(Roles = "Data Contributor")]
    [EnableRateLimiting("upload")]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    [RequestSizeLimit(62914560)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFile(int id)
    {
        var file = Request.Form.Files[0];

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        if (string.IsNullOrEmpty(file.FileName) ||
            !file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only JSON files are accepted." });
        }

        const long maxFileSize = 50 * 1024 * 1024; // 50MB
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { message = "File size must be under 50MB." });
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.Id == id);
        if (volunteer == null)
        {
            return NotFound(new { message = "Data Contributor not found" });
        }

        if (volunteer.UserId != userId)
        {
            return Forbid();
        }

        using var stream = file.OpenReadStream();

        try
        {
            using var jsonDoc = await JsonDocument.ParseAsync(stream);
        }
        catch (JsonException)
        {
            return BadRequest(new { message = "Invalid JSON file. Please upload a valid JSON export." });
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        var command = new ProcessUploadedFileCommand
        {
            VolunteerId = id,
            FileStream = stream,
            FileName = file.FileName,
            FileSize = file.Length
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    // POST: api/v1/DataContributor/upload-file (mobile)
    [HttpPost("upload-file")]
    [Authorize(Roles = "Data Contributor")]
    [EnableRateLimiting("upload")]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    [RequestSizeLimit(62914560)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFileMobile()
    {
        var file = Request.Form.Files[0];

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        if (string.IsNullOrEmpty(file.FileName) ||
            !file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only ZIP files are accepted." });
        }

        const long maxFileSize = 50 * 1024 * 1024; // 50MB
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { message = "File size must be under 50MB." });
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.UserId == userId);
        if (volunteer == null)
        {
            return NotFound(new { message = "Data Contributor not found" });
        }

        using var stream = file.OpenReadStream();
        var command = new ProcessUploadedFileCommand
        {
            VolunteerId = volunteer.Id,
            FileStream = stream,
            FileName = file.FileName,
            FileSize = file.Length
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    // POST: api/v1/contributor/email
    [HttpPost("email")]
    [Authorize(Roles = "Data Contributor")]
    public async Task<IActionResult> CreateEmail([FromBody] ContributorEmailRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        // Validate email format
        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return BadRequest(new { message = "Invalid email format" });
        }

        // Delete any existing email for this contributor
        var existing = await _context.ContributorEmails
            .FirstOrDefaultAsync(e => e.ContributorId == userId);
        if (existing != null)
        {
            _context.ContributorEmails.Remove(existing);
        }

        // Create new email record
        var contributorEmail = new ContributorEmail
        {
            ContributorId = userId,
            Email = request.Email.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _context.ContributorEmails.Add(contributorEmail);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Email saved successfully" });
    }

    // DELETE: api/v1/contributor/email (GDPR email erasure)
    [HttpDelete("email")]
    [Authorize(Roles = "Data Contributor")]
    public async Task<IActionResult> DeleteEmail()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var existing = await _context.ContributorEmails
            .FirstOrDefaultAsync(e => e.ContributorId == userId);
        if (existing == null)
        {
            return NotFound(new { message = "No email found to delete" });
        }

        _context.ContributorEmails.Remove(existing);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Email deleted successfully" });
    }

    // PUT: api/v1/DataContributor/push-token
    [HttpPut("push-token")]
    [Authorize(Roles = "Data Contributor")]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> UpdatePushToken([FromBody] DataContributorUpdatePushTokenRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.UserId == userId);
        if (volunteer == null)
        {
            return NotFound(new { message = "Data Contributor not found" });
        }

        volunteer.PushToken = request.PushToken;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Push token updated successfully" });
    }

    // DELETE: api/v1/contributor/data (full GDPR deletion)
    [HttpDelete("data")]
    [Authorize(Roles = "Data Contributor")]
    public async Task<IActionResult> DeleteAllData()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest(new { message = "Invalid user ID" });
            }

            // 1. Delete WatchEvents
            var watchEvents = await _context.WatchEvents
                .Where(w => w.ContributorId == userGuid)
                .ToListAsync();
            _context.WatchEvents.RemoveRange(watchEvents);

            // 2. Delete Donation
            var donation = await _context.Donations
                .FirstOrDefaultAsync(d => d.ContributorId == userId);
            if (donation != null)
            {
                _context.Donations.Remove(donation);
            }

            // 3. Delete ContributorEmail
            var email = await _context.ContributorEmails
                .FirstOrDefaultAsync(e => e.ContributorId == userId);
            if (email != null)
            {
                _context.ContributorEmails.Remove(email);
            }

            // 4. Anonymize and disable ApplicationUser
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.FullName = "[DELETED]";
                user.Email = $"deleted_{userId}@deleted.nadena";
                user.DeletedAt = DateTime.UtcNow;
                user.UserName = user.Email;
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);

                await _userManager.UpdateAsync(user);

                // Invalidate all sessions
                var sessions = await _context.UserSessions
                    .Where(s => s.UserId == userId)
                    .ToListAsync();
                foreach (var session in sessions)
                {
                    session.ExpiresAt = DateTime.UtcNow;
                }
            }

            // 5. Log audit entry
            await _auditLogService.LogAsync(
                action: "DataDeletion",
                entityType: "User",
                entityId: userId,
                success: true,
                userId: userId,
                newValues: "{\"action\":\"User requested full data deletion\"}");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Your data has been deleted." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            await _auditLogService.LogAsync(
                action: "DataDeletion",
                entityType: "User",
                entityId: userId,
                success: false,
                errorMessage: ex.Message);

            return StatusCode(500, new { message = "Failed to delete data." });
        }
    }

    // DELETE: api/v1/DataContributor/{id}/my-data
    [HttpDelete("{id}/my-data")]
    [Authorize]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> DeleteMyData(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.Id == id);
        if (volunteer == null)
        {
            return NotFound(new { message = "Data Contributor not found" });
        }

        if (volunteer.UserId != userId)
        {
            return Forbid();
        }

        var deletionRequest = new Domain.Entities.DeletionRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VolunteerId = id,
            Status = "Pending",
            Reason = "Contributor requested deletion",
            RequestedAt = DateTime.UtcNow
        };

        _context.DeletionRequests.Add(deletionRequest);
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("DataDeletionRequested", "DeletionRequest", deletionRequest.Id.ToString(), true, userId);
        return Ok(new { message = "Deletion request submitted for admin review.", data = deletionRequest });
    }

    // GET: api/v1/DataContributor/{id}/export-my-data
    // DO NOT include ContributorEmails in this response.
    [HttpGet("{id}/export-my-data")]
    [Authorize]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> ExportMyData(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.Id == id);
        if (volunteer == null)
        {
            return NotFound(new { message = "Data Contributor not found" });
        }

        if (volunteer.UserId != userId)
        {
            return Forbid();
        }

        var query = new ExportMyDataQuery
        {
            VolunteerId = id,
            RequestedByUserId = userId
        };

        return Ok(await _mediator.Send(query));
    }

    [HttpGet("earnings/export-csv")]
    [Authorize(Roles = "Data Contributor")]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> ExportEarningsCsv()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var wallet = await _context.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.OwnerId == userId);
        if (wallet == null)
        {
            return File(Encoding.UTF8.GetBytes("date,type,status,amount,currency,reference\n"), "text/csv", "earnings-history.csv");
        }

        var transactions = await _context.Transactions.AsNoTracking()
            .Where(t => t.ToWalletId == wallet.Id && t.Type == "ContributorPayout")
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("date,type,status,amount,currency,reference");
        foreach (var item in transactions)
        {
            csv.AppendLine($"{item.CreatedAt:yyyy-MM-dd},{item.Type},{item.Status},{item.Amount:F2},{item.Currency},{item.ReferenceId}");
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "earnings-history.csv");
    }

    // GET: api/v1/DataContributor/estimate-value
    [HttpGet("estimate-value")]
    [AllowAnonymous]
    public async Task<IActionResult> EstimateValue([FromQuery] int commentCount, [FromQuery] string contentTypes, [FromQuery] string accountAge)
    {
        var command = new EstimateDataValueCommand
        {
            VolunteerId = 0,
            CommentCountEstimate = commentCount,
            ContentTypes = contentTypes ?? string.Empty,
            YouTubeAccountAge = accountAge ?? string.Empty
        };

        return Ok(await _mediator.Send(command));
    }

    // POST: api/v1/DataContributor/calculate-my-value
    [HttpPost("calculate-my-value")]
    [Authorize]
    [ServiceFilter(typeof(RequireDataContributorOnboardingFilter))]
    public async Task<IActionResult> CalculateMyValue([FromBody] DataContributorCalculateMyValueRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.UserId == userId);
        if (volunteer == null)
        {
            return NotFound(new { message = "Data Contributor not found" });
        }

        var command = new EstimateDataValueCommand
        {
            VolunteerId = volunteer.Id,
            CommentCountEstimate = request.CommentCountEstimate,
            ContentTypes = request.ContentTypes ?? string.Empty,
            YouTubeAccountAge = request.YouTubeAccountAge ?? string.Empty
        };

        return Ok(await _mediator.Send(command));
    }
}

public class DataContributorCalculateMyValueRequest
{
    public int CommentCountEstimate { get; set; }
    public string? ContentTypes { get; set; }
    public string? YouTubeAccountAge { get; set; }
}

public class DataContributorUpdatePushTokenRequest
{
    public string? PushToken { get; set; }
}

public class DataContributorDataSourcesRequest
{
    public DataSourceSettings? YouTube { get; set; }
    public DataSourceSettings? Spotify { get; set; }
    public DataSourceSettings? Netflix { get; set; }
}

public class DataSourceSettings
{
    public bool Enabled { get; set; }
    public string SharingPreference { get; set; } = "ShareNow"; // ShareNow | WaitForRequest
}

internal static class NotesJson
{
    public static Dictionary<string, object?> TryParseObject(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }

            return ToDictionary(doc.RootElement);
        }
        catch
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static Dictionary<string, object?> ToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.Object => ToDictionary(prop.Value),
                JsonValueKind.Array => prop.Value.EnumerateArray().Select(v => v.ToString()).ToList(),
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number => prop.Value.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null
            };
        }
        return dict;
    }
}

public class ContributorEmailRequest
{
    public string Email { get; set; } = string.Empty;
}

public class UpdateEmailRequest
{
    public string Email { get; set; } = string.Empty;
}
