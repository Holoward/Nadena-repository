using Application.Common;
using Application.Features.Buyers.Commands.CreateBuyer;
using Application.Features.Buyers.Commands.CreateCheckoutSession;
using Application.Features.Buyers.Queries.GetAllBuyers;
using Application.Features.Buyers.Queries.GetBuyerByUserId;
using Application.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/v{version:apiVersion}/[controller]")]
public class BuyerController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRepositoryAsync<DatasetPurchase> _purchaseRepository;

    public BuyerController(
        ISender mediator,
        ICurrentUserService currentUserService,
        IRepositoryAsync<DatasetPurchase> purchaseRepository)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _purchaseRepository = purchaseRepository;
    }

    // GET: api/v1/Buyer
    [HttpGet]
    [Authorize(Roles = "Data Client,Admin")]
    public async Task<IActionResult> Get([FromQuery] int page = 1, int pageSize = 20)
    {
        var paginationParams = new PaginationParams
        {
            Page = page,
            PageSize = pageSize
        };
        return Ok(await _mediator.Send(new GetAllBuyersQuery { PaginationParams = paginationParams }));
    }

    // GET: api/v1/Buyer/user/some-guid
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Data Client,Admin")]
    public async Task<IActionResult> GetByUserId(Guid userId)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        
        // Verify the current user is the same as the requested userId
        if (currentUserId != userId.ToString())
        {
            return Forbid();
        }
        
        return Ok(await _mediator.Send(new GetBuyerByUserIdQuery { UserId = userId }));
    }

    // POST: api/v1/Buyer/setup
    [HttpPost("setup")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> SetupData([FromBody] Application.Features.Buyers.Commands.SetupBuyerData.SetupBuyerDataCommand command)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }
        
        command.UserId = userId; // Force the command to use the authenticated user's ID
        
        return Ok(await _mediator.Send(command));
    }

    // POST: api/v1/Buyer
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Post(CreateBuyerCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    // POST: api/v1/Buyer/checkout
    [HttpPost("checkout")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var buyerUserId = _currentUserService.GetCurrentUserId();
        
        var command = new CreateCheckoutSessionCommand
        {
            DatasetId = request.DatasetId,
            BuyerUserId = buyerUserId
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { message = response.Message });
        }

        return Ok(new { url = response.Data });
    }
    /// <summary>
    /// Sets or updates the webhook endpoint that Nadena will POST data to when
    /// new contributor records are available for this purchase.
    /// Only the buyer who owns the purchase may configure this.
    /// </summary>
    // PATCH: api/v1/Buyer/purchases/{purchaseId}/delivery-endpoint
    [HttpPatch("purchases/{purchaseId:guid}/delivery-endpoint")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> SetDeliveryEndpoint(
        Guid purchaseId,
        [FromBody] SetDeliveryEndpointRequest request)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        // DatasetPurchase has a Guid PK — fetch via ListAsync and filter.
        var all = await _purchaseRepository.ListAsync();
        var target = all.FirstOrDefault(p => p.Id == purchaseId);

        if (target is null)
            return NotFound(new { message = "Purchase not found." });

        // Security: only the owning buyer may configure delivery.
        if (target.BuyerId.ToString() != userId)
            return Forbid();

        // Validate URL (must be absolute https or http)
        if (!string.IsNullOrWhiteSpace(request.DeliveryEndpoint) &&
            !Uri.TryCreate(request.DeliveryEndpoint, UriKind.Absolute, out _))
        {
            return BadRequest(new { message = "deliveryEndpoint must be a valid absolute URL." });
        }

        target.DeliveryEndpoint = string.IsNullOrWhiteSpace(request.DeliveryEndpoint)
            ? null
            : request.DeliveryEndpoint.Trim();

        await _purchaseRepository.UpdateAsync(target);

        return Ok(new
        {
            message = "Delivery endpoint updated.",
            purchaseId,
            deliveryEndpoint = target.DeliveryEndpoint
        });
    }
}

public class CheckoutRequest
{
    public Guid DatasetId { get; set; }
}

public class SetDeliveryEndpointRequest
{
    /// <summary>
    /// Absolute URL Nadena will POST anonymized records to.
    /// Set to null or empty to clear an existing endpoint.
    /// </summary>
    public string? DeliveryEndpoint { get; set; }
}
