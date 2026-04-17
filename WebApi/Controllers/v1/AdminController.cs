using Application.Features.Admin.Commands.PayVolunteers;
using Application.Features.Admin.Commands.ProcessRefund;
using Application.Features.Admin.Queries.GetAuditLogs;
using Application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Domain.Enums;
using Application.Interfaces;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ApplicationDbContext _dbContext;
    private readonly IPaymentService _paymentService;

    public AdminController(ISender mediator, ApplicationDbContext dbContext, IPaymentService paymentService)
    {
        _mediator = mediator;
        _dbContext = dbContext;
        _paymentService = paymentService;
    }

    // GET: api/v1/admin/audit-logs
    [HttpGet("audit-logs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetAuditLogsQuery
        {
            UserId = userId,
            Action = action,
            From = from,
            To = to,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    // POST: api/v1/Admin/pay-volunteers
    [HttpPost("pay-volunteers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PayVolunteers([FromBody] PayVolunteersCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    // POST: api/v1/Admin/process-refund
    [HttpPost("process-refund")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ProcessRefund([FromBody] ProcessRefundCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    // GET: api/v1/Admin/flagged-datasets
    [HttpGet("flagged-datasets")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetFlaggedDatasets()
    {
        var datasets = await _dbContext.Datasets
            .Where(d => d.IntegrityStatus == IntegrityStatus.Flagged)
            .Select(d => new
            {
                d.Id,
                d.Title,
                d.IntegrityReason,
                d.DataIntegrityHash,
                d.Created,
                ContributorCount = d.VolunteerCount
            })
            .ToListAsync();

        return Ok(new { data = datasets });
    }

    // POST: api/v1/Admin/flagged-datasets/{id}/clear
    [HttpPost("flagged-datasets/{id}/clear")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ClearFlag(int id)
    {
        var dataset = await _dbContext.Datasets.FirstOrDefaultAsync(d => d.Id == id);
        if (dataset == null) return NotFound(new { message = "Dataset not found" });

        dataset.IntegrityStatus = IntegrityStatus.Verified;
        dataset.IntegrityReason = null;
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Dataset cleared." });
    }

    // POST: api/v1/Admin/flagged-datasets/{id}/reject
    [HttpPost("flagged-datasets/{id}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectFlag(int id)
    {
        var dataset = await _dbContext.Datasets.FirstOrDefaultAsync(d => d.Id == id);
        if (dataset == null) return NotFound(new { message = "Dataset not found" });

        dataset.IntegrityStatus = IntegrityStatus.Flagged;
        dataset.IntegrityReason = "Rejected by admin review";
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Dataset rejected and remains flagged." });
    }

    [HttpGet("platform-wallet")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PlatformWallet()
    {
        var wallet = await _dbContext.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.OwnerId == "platform");
        if (wallet == null)
        {
            return Ok(new { data = new { balance = 0m, pendingBalance = 0m, currency = "USD" } });
        }

        return Ok(new { data = wallet });
    }

    [HttpGet("pending-payouts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PendingPayouts()
    {
        var payouts = await _dbContext.Transactions.AsNoTracking()
            .Where(t => t.Type == "ContributorPayout")
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var payoutWalletIds = payouts.Select(t => t.ToWalletId).Distinct().ToList();
        var wallets = await _dbContext.Wallets.AsNoTracking()
            .Where(w => payoutWalletIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w);

        var userIds = wallets.Values.Select(w => w.OwnerId).Distinct().ToList();
        var users = await _dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var disbursed = await _dbContext.ContributorDisbursements.AsNoTracking()
            .Select(d => d.TransactionId)
            .ToListAsync();

        var data = payouts
            .Where(t => !disbursed.Contains(t.Id))
            .Select(t => new
            {
                t.Id,
                t.Amount,
                t.Status,
                t.CreatedAt,
                t.CompletedAt,
                t.ReferenceId,
                ContributorName = wallets.TryGetValue(t.ToWalletId, out var wallet) && users.TryGetValue(wallet.OwnerId, out var fullName)
                    ? fullName
                    : wallet?.OwnerId ?? "Unknown"
            })
            .ToList();

        return Ok(new { data });
    }

    [HttpPost("pending-payouts/{transactionId:guid}/mark-disbursed")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MarkPayoutDisbursed(Guid transactionId, [FromBody] MarkDisbursedRequest? request)
    {
        var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "admin";
        var result = await _paymentService.MarkDisbursedExternallyAsync(transactionId, adminUserId, request?.Notes);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message, disbursementId = result.Data });
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users([FromQuery] string? role = null)
    {
        var query = _dbContext.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(user => user.Role == role);
        }

        var users = await query
            .OrderBy(user => user.Email)
            .Select(user => new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.EmailConfirmed,
                user.CompanyName,
                user.CompanyVerified,
                user.IsSuspended,
                user.DeletedAt
            })
            .ToListAsync();

        return Ok(new { data = users });
    }

    [HttpPost("users/{id}/suspend")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SuspendUser(string id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(entry => entry.Id == id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        user.IsSuspended = true;
        await _dbContext.SaveChangesAsync();
        return Ok(new { message = "User suspended." });
    }

    [HttpPost("users/{id}/reactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReactivateUser(string id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(entry => entry.Id == id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        user.IsSuspended = false;
        user.DeletedAt = null;
        await _dbContext.SaveChangesAsync();
        return Ok(new { message = "User reactivated." });
    }

    [HttpGet("consent-records")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ConsentRecords([FromQuery] string? userId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var query = _dbContext.ConsentRecords.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(record => record.UserId == userId);
        }

        if (from.HasValue)
        {
            query = query.Where(record => record.AgreedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(record => record.AgreedAt <= to.Value);
        }

        var records = await query.OrderByDescending(record => record.AgreedAt).ToListAsync();
        return Ok(new { data = records });
    }

    // GET: api/v1/Admin/revenue-report
    // DO NOT include ContributorEmails in this response.
    [HttpGet("revenue-report")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RevenueReport([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] bool exportCsv = false)
    {
        var query = _dbContext.Transactions.AsNoTracking().AsQueryable();
        if (from.HasValue)
        {
            query = query.Where(transaction => transaction.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(transaction => transaction.CreatedAt <= to.Value);
        }

        var transactions = await query.ToListAsync();
        var report = new
        {
            totalRevenue = transactions.Where(transaction => transaction.Type == "DataPurchase" && transaction.Status == "Completed").Sum(transaction => transaction.Amount),
            platformFees = transactions.Where(transaction => transaction.Type == "PlatformFee" && transaction.Status == "Completed").Sum(transaction => transaction.Amount),
            contributorPayouts = transactions.Where(transaction => transaction.Type == "ContributorPayout" && transaction.Status == "Completed").Sum(transaction => transaction.Amount)
        };

        if (!exportCsv)
        {
            return Ok(new { data = report });
        }

        var csv = $"metric,amount{Environment.NewLine}totalRevenue,{report.totalRevenue:F2}{Environment.NewLine}platformFees,{report.platformFees:F2}{Environment.NewLine}contributorPayouts,{report.contributorPayouts:F2}{Environment.NewLine}";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "revenue-report.csv");
    }

    [HttpGet("deletion-requests")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletionRequests()
    {
        var requests = await _dbContext.DeletionRequests.AsNoTracking()
            .OrderByDescending(request => request.RequestedAt)
            .ToListAsync();
        return Ok(new { data = requests });
    }

    [HttpPost("deletion-requests/{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveDeletionRequest(Guid id)
    {
        var request = await _dbContext.DeletionRequests.FirstOrDefaultAsync(entry => entry.Id == id);
        if (request == null)
        {
            return NotFound(new { message = "Deletion request not found." });
        }

        request.Status = "Approved";
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        request.ReviewNotes = "Approved by admin";

        if (request.VolunteerId.HasValue)
        {
            var volunteer = await _dbContext.Volunteers.FirstOrDefaultAsync(entry => entry.Id == request.VolunteerId.Value);
            if (volunteer != null)
            {
                volunteer.Status = VolunteerStatus.Deleted;
            }

            var affectedPurchases = await _dbContext.DatasetPurchases.Where(purchase => purchase.Status != "Cancelled").ToListAsync();
            foreach (var purchase in affectedPurchases)
            {
                purchase.Status = "ReviewRequired";
            }
        }

        await _dbContext.SaveChangesAsync();
        return Ok(new { message = "Deletion request approved." });
    }

    [HttpPost("deletion-requests/{id:guid}/deny")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DenyDeletionRequest(Guid id)
    {
        var request = await _dbContext.DeletionRequests.FirstOrDefaultAsync(entry => entry.Id == id);
        if (request == null)
        {
            return NotFound(new { message = "Deletion request not found." });
        }

        request.Status = "Denied";
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        request.ReviewNotes = "Denied by admin";
        await _dbContext.SaveChangesAsync();
        return Ok(new { message = "Deletion request denied." });
    }

    [HttpGet("email-logs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EmailLogs()
    {
        var emails = await _dbContext.EmailLogs.AsNoTracking()
            .OrderByDescending(email => email.SentAt)
            .Take(500)
            .ToListAsync();
        return Ok(new { data = emails });
    }

    [HttpGet("company-verification-queue")]
    [Authorize(Roles = "Admin")]
    public IActionResult CompanyVerificationQueue()
        => Ok(new { data = Array.Empty<object>() });
}

public class PayVolunteersRequest
{
    public List<Guid> VolunteerIds { get; set; } = new();
    public Guid DatasetId { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class MarkDisbursedRequest
{
    public string? Notes { get; set; }
}
