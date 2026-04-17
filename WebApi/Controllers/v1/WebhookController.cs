using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IRepositoryAsync<DatasetPurchase> _purchaseRepository;
    private readonly IRepositoryAsync<Dataset> _datasetRepository;
    private readonly IRepositoryAsync<Buyer> _buyerRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<WebhookController> _logger;
    private readonly ILicensePdfService _licensePdfService;
    private readonly IEmailService _emailService;

    public WebhookController(
        IConfiguration configuration,
        IRepositoryAsync<DatasetPurchase> purchaseRepository,
        IRepositoryAsync<Dataset> datasetRepository,
        IRepositoryAsync<Buyer> buyerRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<WebhookController> logger,
        ILicensePdfService licensePdfService,
        IEmailService emailService)
    {
        _configuration = configuration;
        _purchaseRepository = purchaseRepository;
        _datasetRepository = datasetRepository;
        _buyerRepository = buyerRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _licensePdfService = licensePdfService;
        _emailService = emailService;
    }

    [HttpPost("stripe")]
    [Consumes("application/json")]
    public async Task<IActionResult> StripeWebhook()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return StatusCode(500, "HttpContext is not available");
        }

        var request = httpContext.Request;
        
        // Read the raw request body
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();

        // Get the Stripe signature header
        var signatureHeader = request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signatureHeader))
        {
            _logger.LogWarning("Missing Stripe-Signature header");
            return BadRequest("Missing Stripe-Signature header");
        }

        // Get the webhook secret from configuration
        var webhookSecret = _configuration["NadenaSettings:StripeWebhookSecret"];

        if (string.IsNullOrEmpty(webhookSecret))
        {
            _logger.LogError("StripeWebhookSecret is not configured");
            return StatusCode(500, "Stripe webhook secret not configured");
        }

        Event stripeEvent;

        try
        {
            // Verify the Stripe signature
            stripeEvent = EventUtility.ConstructEvent(body, signatureHeader, webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe signature");
            return BadRequest($"Invalid Stripe signature: {ex.Message}");
        }

        // Handle the checkout.session.completed event
        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;

            if (session != null)
            {
                try
                {
                    // Extract datasetId and buyerId from metadata
                    var metadata = session.Metadata;
                    
                    if (!metadata.TryGetValue("datasetId", out var datasetIdStr) ||
                        !metadata.TryGetValue("buyerId", out var buyerIdStr))
                    {
                        _logger.LogWarning("Missing metadata in Stripe session");
                        return BadRequest("Missing metadata in Stripe session");
                    }

                    if (!Guid.TryParse(datasetIdStr, out var datasetId) ||
                        !Guid.TryParse(buyerIdStr, out var buyerId))
                    {
                        _logger.LogWarning("Invalid metadata format in Stripe session");
                        return BadRequest("Invalid metadata format in Stripe session");
                    }

                    // Verify dataset exists
                    var dataset = await _datasetRepository.GetByIdAsync(datasetId);
                    if (dataset == null)
                    {
                        _logger.LogWarning("Dataset not found: {DatasetId}", datasetId);
                        return BadRequest("Dataset not found");
                    }

                    // Verify buyer exists
                    var buyer = await _buyerRepository.GetByIdAsync(buyerId);
                    if (buyer == null)
                    {
                        _logger.LogWarning("Data Client not found: {BuyerId}", buyerId);
                        return BadRequest("Data Client not found");
                    }

                    // Generate the signed download URL
                    var token = Guid.NewGuid().ToString();
                    var baseUrl = _configuration["NadenaSettings:FrontendUrl"] ?? "http://localhost:3000";
                    var downloadUrl = $"{baseUrl}/api/v1/Dataset/{datasetId}/download?token={token}";

                    // Create the DatasetPurchase record
                    var purchase = new DatasetPurchase
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyerId,
                        DatasetId = datasetId,
                        StripeSessionId = session.Id,
                        AmountPaid = (session.AmountTotal ?? 0) / 100m,
                        PurchasedAt = DateTime.UtcNow,
                        DownloadUrl = downloadUrl,
                        DownloadExpiry = DateTime.UtcNow.AddHours(48),
                        CreatedBy = buyerId.ToString(),
                        Created = DateTime.UtcNow
                    };

                    // Save the purchase record
                    await _purchaseRepository.AddAsync(purchase);

                    // If this is a recurring subscription, create a DatasetSubscription record
                    if (dataset.PricingModel == "Monthly" || dataset.PricingModel == "Quarterly")
                    {
                        var subscription = new DatasetSubscription
                        {
                            DatasetId = datasetId,
                            BuyerId = buyerId,
                            StripeSubscriptionId = session.SubscriptionId ?? session.Id,
                            PricingModel = dataset.PricingModel,
                            StartDate = DateTime.UtcNow,
                            NextDeliveryDate = dataset.PricingModel == "Monthly"
                                ? DateTime.UtcNow.AddMonths(1)
                                : DateTime.UtcNow.AddMonths(3),
                            IsActive = true,
                            CreatedBy = buyerId.ToString()
                        };

                        var subscriptionRepo = HttpContext.RequestServices.GetRequiredService<IRepositoryAsync<DatasetSubscription>>();
                        await subscriptionRepo.AddAsync(subscription);
                    }

                    // Generate license PDF and send download confirmation email with attachment
                    var pdfBytes = _licensePdfService.GenerateLicensePdf(buyer.CompanyName ?? "Data Client", buyer.CompanyName, dataset.Id, purchase.PurchasedAt);
                    var buyerEmail = session.CustomerEmail ?? buyer.UserId;
                    await _emailService.SendDownloadConfirmationEmailAsync(buyerEmail, buyer.CompanyName ?? "Data Client", dataset.Id, pdfBytes, $"Nadena_License_{dataset.Id}.pdf");

                    _logger.LogInformation(
                        "Purchase completed: BuyerId={BuyerId}, DatasetId={DatasetId}, SessionId={SessionId}",
                        buyerId, datasetId, session.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Stripe webhook");
                    return StatusCode(500, "Error processing webhook");
                }
            }
        }

        return Ok();
    }
}
