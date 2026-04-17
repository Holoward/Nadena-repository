using Application.Features.Licensing.Commands.PurchaseLicense;
using Application.Features.Licensing.Commands.RevokeLicense;
using Application.Features.Licensing.Queries.GetMyLicenses;
using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class LicensingController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ICurrentUserService _currentUserService;

    public LicensingController(ISender mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Purchase an API license for a DataPool.
    /// Returns the raw API key exactly ONCE — the data client must save it.
    /// Revenue is automatically split to data contributors upon purchase.
    /// </summary>
    // POST: api/v1/Licensing/purchase
    [HttpPost("purchase")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> Purchase([FromBody] PurchaseLicenseRequest request)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new PurchaseLicenseCommand
        {
            DataPoolId = request.DataPoolId,
            BuyerUserId = userId,
            Months = request.Months
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new
        {
            message = "License purchased. Save your API key — it will not be shown again.",
            result.Data
        });
    }

    // GET: api/v1/Licensing/my-licenses
    [HttpGet("my-licenses")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> GetMyLicenses()
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetMyLicensesQuery
        {
            BuyerUserId = Guid.Parse(userId)
        });

        return Ok(result);
    }

    // POST: api/v1/Licensing/revoke/{id} — Admin only
    [HttpPost("revoke/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var result = await _mediator.Send(new RevokeLicenseCommand { LicenseId = id });

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = "License revoked and API key disabled." });
    }
}

public class PurchaseLicenseRequest
{
    public int DataPoolId { get; set; }

    /// <summary>Number of months. Default 1, max 24.</summary>
    public int Months { get; set; } = 1;
}
