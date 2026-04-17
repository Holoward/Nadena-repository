using Application.Features.Takeout.Commands;
using Application.Features.Takeout.Queries;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[EnableRateLimiting("api")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class TakeoutController : ControllerBase
{
    private readonly ISender _sender;

    public TakeoutController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("upload")]
    [EnableCors("ChromeExtension")]
    [EnableRateLimiting("upload")]
    [RequestSizeLimit(104857600)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile zipFile,
        CancellationToken cancellationToken)
    {
        if (zipFile is null || zipFile.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        if (!zipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only ZIP files are accepted." });
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var contributorId))
        {
            return Unauthorized(new { message = "Invalid or missing user identity." });
        }

        await using var fileStream = zipFile.OpenReadStream();

        var result = await _sender.Send(new ProcessTakeoutCommand
        {
            ZipFileStream = fileStream,
            ContributorId = contributorId
        }, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("stats/{contributorId:guid}")]
    public async Task<IActionResult> GetStats(Guid contributorId, CancellationToken cancellationToken)
    {
        if (contributorId == Guid.Empty)
        {
            return BadRequest(new { message = "A valid contributorId is required." });
        }

        var result = await _sender.Send(new GetContributorStatsQuery
        {
            ContributorId = contributorId
        }, cancellationToken);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}