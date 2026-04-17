using Application.Common;
using Application.Features.Buyers.Commands.CreateCheckoutSession;
using Application.Features.Buyers.Commands.CreateBuyer;
using Application.Features.Buyers.Queries.GetAllBuyers;
using Application.Features.Buyers.Queries.GetBuyerByUserId;
using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using System.Text.Json;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DataClientController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPaymentService _paymentService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly ILicensePdfService _licensePdfService;
    private readonly IEmailService _emailService;

    public DataClientController(
        ISender mediator,
        ICurrentUserService currentUserService,
        IPaymentService paymentService,
        ApplicationDbContext dbContext,
        IWebHostEnvironment environment,
        ILicensePdfService licensePdfService,
        IEmailService emailService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _paymentService = paymentService;
        _dbContext = dbContext;
        _environment = environment;
        _licensePdfService = licensePdfService;
        _emailService = emailService;
    }

    // GET: api/v1/DataClient
    [HttpGet]
    [Authorize(Roles = "Data Client,Admin")]
    public async Task<IActionResult> Get([FromQuery] int page = 1, int pageSize = 20)
    {
        var paginationParams = new PaginationParams { Page = page, PageSize = pageSize };
        return Ok(await _mediator.Send(new GetAllBuyersQuery { PaginationParams = paginationParams }));
    }

    // GET: api/v1/DataClient/user/{userId}
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Data Client,Admin")]
    public async Task<IActionResult> GetByUserId(Guid userId)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();

        if (currentUserId != userId.ToString())
        {
            return Forbid();
        }

        return Ok(await _mediator.Send(new GetBuyerByUserIdQuery { UserId = userId }));
    }

    // POST: api/v1/DataClient/setup
    [HttpPost("setup")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> SetupData([FromBody] Application.Features.Buyers.Commands.SetupBuyerData.SetupBuyerDataCommand command)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        command.UserId = userId;
        return Ok(await _mediator.Send(command));
    }

    // POST: api/v1/DataClient — Admin only
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Post(CreateBuyerCommand command)
        => Ok(await _mediator.Send(command));

    // POST: api/v1/DataClient/checkout
    [HttpPost("checkout")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> Checkout([FromBody] DataClientCheckoutRequest request)
    {
        var buyerUserId = _currentUserService.GetCurrentUserId();
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == buyerUserId);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new { message = "Idempotency-Key header is required." });
        }

        if (user == null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        if (!user.EmailConfirmed)
        {
            return BadRequest(new { message = "Please verify your email before making purchases." });
        }

        var response = await _paymentService.ProcessPurchaseAsync(
            dataClientUserId: Guid.Parse(buyerUserId),
            datasetId: request.DatasetId,
            price: request.Price,
            billingType: request.BillingType,
            idempotencyKey: idempotencyKey,
            contributorShareNow: request.ContributorShareNow);

        if (!response.Success)
        {
            return BadRequest(new { message = response.Message });
        }

        return Ok(new { message = "Purchase confirmed", transactionId = response.Data });
    }

    [HttpGet("wallet")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> Wallet()
    {
        var buyerUserId = _currentUserService.GetCurrentUserId();
        var wallet = await _dbContext.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.OwnerId == buyerUserId);
        if (wallet == null)
        {
            return Ok(new { data = new { balance = 0m, pendingBalance = 0m, currency = "USD" } });
        }

        return Ok(new { data = wallet });
    }

    [HttpGet("transactions")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> Transactions()
    {
        var buyerUserId = _currentUserService.GetCurrentUserId();
        var wallet = await _dbContext.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.OwnerId == buyerUserId);
        if (wallet == null)
        {
            return Ok(new { data = Array.Empty<object>() });
        }

        var transactions = await _dbContext.Transactions.AsNoTracking()
            .Where(t => t.FromWalletId == wallet.Id || t.ToWalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(new { data = transactions });
    }

    [HttpPost("purchases")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> PurchaseDataset([FromBody] DataClientPurchaseRequest request)
    {
        var buyerUserId = _currentUserService.GetCurrentUserId();
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new { message = "Idempotency-Key header is required." });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(entry => entry.Id == buyerUserId);
        if (user == null)
        {
            return Unauthorized();
        }

        if (!user.EmailConfirmed)
        {
            return BadRequest(new { message = "Please verify your email before making purchases." });
        }

        var datasetId = Guid.NewGuid();
        var computedPrice = CalculatePrice(request.RecordCount, request.PurchaseType);
        var response = await _paymentService.ProcessPurchaseAsync(Guid.Parse(buyerUserId), datasetId, computedPrice, request.PurchaseType, idempotencyKey, request.ContributorShareNow);
        if (!response.Success)
        {
            return BadRequest(new { message = response.Message });
        }

        var downloadToken = Guid.NewGuid();
        var frontendBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var downloadUrl = $"{frontendBaseUrl}/api/v1/Dataset/{datasetId}/download?token={downloadToken}";
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var purchase = new Domain.Entities.DatasetPurchase
        {
            Id = Guid.NewGuid(),
            BuyerId = Guid.Parse(buyerUserId),
            DatasetId = datasetId,
            StripeSessionId = response.Data ?? string.Empty,
            AmountPaid = computedPrice,
            PurchasedAt = DateTime.UtcNow,
            DownloadUrl = downloadUrl,
            DownloadExpiry = DateTime.UtcNow.AddHours(1),
            PurchaseType = request.PurchaseType == "OneTime" ? "One-time" : "Recurring",
            BillingFrequency = request.PurchaseType,
            Status = "Ready",
            RecordCount = request.RecordCount,
            DataSources = string.Join(", ", request.DataSources ?? new List<string>()),
            DateRangeStart = request.DateRangeStart,
            DateRangeEnd = request.DateRangeEnd,
            InvoiceNumber = invoiceNumber,
            RefreshCount = 1,
            NextRefreshDate = request.PurchaseType == "OneTime" ? null : GetNextRefreshDate(request.PurchaseType),
            LastRefreshedAt = DateTime.UtcNow,
            MetricsHistoryJson = JsonSerializer.Serialize(new[]
            {
                new { date = DateTime.UtcNow.ToString("yyyy-MM-dd"), count = request.RecordCount }
            })
        };

        _dbContext.DatasetPurchases.Add(purchase);

        if (request.PurchaseType != "OneTime")
        {
            _dbContext.DatasetSubscriptions.Add(new Domain.Entities.DatasetSubscription
            {
                DatasetId = datasetId,
                BuyerId = Guid.Parse(buyerUserId),
                StripeSubscriptionId = response.Data ?? string.Empty,
                PricingModel = request.PurchaseType,
                StartDate = DateTime.UtcNow,
                NextDeliveryDate = purchase.NextRefreshDate,
                IsActive = true,
                RefreshCount = 1,
                LastDeliveredAt = DateTime.UtcNow
            });
        }

        await WriteDatasetFileAsync(datasetId, request);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Purchase confirmed",
            data = new
            {
                purchase.Id,
                purchase.InvoiceNumber,
                purchase.DownloadUrl,
                purchase.AmountPaid
            }
        });
    }

    [HttpPost("request-custom-quote")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> RequestCustomQuote([FromBody] CustomQuoteRequest request)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(entry => entry.Id == _currentUserService.GetCurrentUserId());
        if (user == null)
        {
            return Unauthorized();
        }

        await _emailService.SendCustomQuoteRequestAsync(user.Email ?? user.UserName ?? "client@nadena.local", request.DatasetName, request.RecordCount);
        return Ok(new { message = "Custom quote request captured." });
    }

    [HttpGet("my-datasets")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> MyDatasets()
    {
        var buyerUserId = _currentUserService.GetCurrentUserId();
        var purchases = await _dbContext.DatasetPurchases.AsNoTracking()
            .Where(purchase => purchase.BuyerId == Guid.Parse(buyerUserId))
            .OrderByDescending(purchase => purchase.PurchasedAt)
            .ToListAsync();

        return Ok(new { data = purchases });
    }

    [HttpPost("my-datasets/{purchaseId:guid}/cancel")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> CancelSubscription(Guid purchaseId)
    {
        var buyerUserId = _currentUserService.GetCurrentUserId();
        var purchase = await _dbContext.DatasetPurchases.FirstOrDefaultAsync(entry => entry.Id == purchaseId && entry.BuyerId == Guid.Parse(buyerUserId));
        if (purchase == null)
        {
            return NotFound(new { message = "Purchase not found." });
        }

        var subscription = await _dbContext.DatasetSubscriptions.FirstOrDefaultAsync(entry => entry.DatasetId == purchase.DatasetId && entry.BuyerId == purchase.BuyerId && entry.IsActive);
        if (subscription == null)
        {
            return BadRequest(new { message = "No active subscription found." });
        }

        subscription.IsActive = false;
        purchase.Status = "Cancelled";
        await _dbContext.SaveChangesAsync();
        return Ok(new { message = "Subscription cancelled." });
    }

    [HttpGet("my-datasets/{purchaseId:guid}/invoice")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> Invoice(Guid purchaseId)
    {
        var buyerUserId = _currentUserService.GetCurrentUserId();
        var purchase = await _dbContext.DatasetPurchases.AsNoTracking().FirstOrDefaultAsync(entry => entry.Id == purchaseId && entry.BuyerId == Guid.Parse(buyerUserId));
        if (purchase == null)
        {
            return NotFound(new { message = "Purchase not found." });
        }

        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(entry => entry.Id == buyerUserId);
        var pdf = _licensePdfService.GenerateLicensePdf(user?.FullName ?? "Data Client", user?.CompanyName ?? string.Empty, 0, purchase.PurchasedAt);
        return File(pdf, "application/pdf", $"{purchase.InvoiceNumber}.pdf");
    }

    [HttpPost("my-datasets/{purchaseId:guid}/share")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> ShareDataset(Guid purchaseId, [FromBody] ShareDatasetRequest request)
    {
        var buyerUserId = _currentUserService.GetCurrentUserId();
        var purchase = await _dbContext.DatasetPurchases.AsNoTracking().FirstOrDefaultAsync(entry => entry.Id == purchaseId && entry.BuyerId == Guid.Parse(buyerUserId));
        if (purchase == null)
        {
            return NotFound(new { message = "Purchase not found." });
        }

        var grant = new Domain.Entities.DatasetAccessGrant
        {
            Id = Guid.NewGuid(),
            DatasetPurchaseId = purchaseId,
            GrantedByUserId = buyerUserId,
            TeammateEmail = request.Email,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _dbContext.DatasetAccessGrants.Add(grant);
        await _dbContext.SaveChangesAsync();
        return Ok(new { message = "Dataset access granted.", data = grant });
    }

    private static decimal CalculatePrice(int recordCount, string purchaseType)
    {
        var baseUnits = Math.Max(recordCount, 100) / 1000m;
        var basePrice = baseUnits * 2.00m;
        return purchaseType switch
        {
            "Daily" => Math.Round(basePrice * 0.8m * 30m, 2),
            "Weekly" => Math.Round(basePrice * 0.75m * 4m, 2),
            "Monthly" => Math.Round(basePrice * 0.7m, 2),
            "Annual" => Math.Round(basePrice * 0.6m * 12m, 2),
            _ => Math.Round(basePrice, 2)
        };
    }

    private static DateTime? GetNextRefreshDate(string purchaseType) => purchaseType switch
    {
        "Daily" => DateTime.UtcNow.AddDays(1),
        "Weekly" => DateTime.UtcNow.AddDays(7),
        "Monthly" => DateTime.UtcNow.AddMonths(1),
        "Annual" => DateTime.UtcNow.AddYears(1),
        _ => null
    };

    private async Task WriteDatasetFileAsync(Guid datasetId, DataClientPurchaseRequest request)
    {
        var directory = Path.Combine(_environment.WebRootPath ?? "wwwroot", "datasets");
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"{datasetId}.csv");
        var rows = new List<string>
        {
            "record_id,source,category,date_range_start,date_range_end,anonymized",
            $"1,\"{string.Join("|", request.DataSources ?? new List<string>())}\",\"{request.Category ?? "General"}\",\"{request.DateRangeStart:yyyy-MM-dd}\",\"{request.DateRangeEnd:yyyy-MM-dd}\",true",
            $"2,\"{string.Join("|", request.DataSources ?? new List<string>())}\",\"{request.Category ?? "General"}\",\"{request.DateRangeStart:yyyy-MM-dd}\",\"{request.DateRangeEnd:yyyy-MM-dd}\",true"
        };

        await System.IO.File.WriteAllLinesAsync(filePath, rows);
    }
}

public class DataClientCheckoutRequest
{
    public Guid DatasetId { get; set; }
    public decimal Price { get; set; }
    public string BillingType { get; set; } = "OneTime";
    public bool ContributorShareNow { get; set; } = true;
}

public class DataClientPurchaseRequest
{
    public string PoolName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string PurchaseType { get; set; } = "OneTime";
    public List<string>? DataSources { get; set; }
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
    public int RecordCount { get; set; } = 1000;
    public bool ContributorShareNow { get; set; } = true;
}

public class ShareDatasetRequest
{
    public string Email { get; set; } = string.Empty;
}

public class CustomQuoteRequest
{
    public string DatasetName { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}
