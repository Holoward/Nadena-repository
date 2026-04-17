using Application.Common;
using Application.Features.Buyers.Commands.CreateBuyer;
using Application.Features.Buyers.Commands.CreateCheckoutSession;
using Application.Features.Buyers.Queries.GetAllBuyers;
using Application.Features.Buyers.Queries.GetBuyerByUserId;
using Application.Interfaces;
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

    public BuyerController(ISender mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
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
}

public class CheckoutRequest
{
    public Guid DatasetId { get; set; }
}
