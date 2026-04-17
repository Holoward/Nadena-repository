using Application.Common;
using Application.Features.Volunteers.Commands.CreateVolunteer;
using Application.Features.Volunteers.Commands.EstimateDataValue;
using Application.Features.Volunteers.Commands.ProcessUploadedFile;
using Application.Features.Volunteers.Commands.RequestDataDeletion;
using Application.Features.Volunteers.Commands.UpdateVolunteerStatus;
using Application.Features.Volunteers.Queries.ExportMyData;
using Application.Features.Volunteers.Queries.GetAllVolunteers;
using Application.Features.Volunteers.Queries.GetVolunteerById;
using Application.Features.Volunteers.Queries.GetVolunteerByUserId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/v{version:apiVersion}/[controller]")]
public class VolunteerController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ApplicationDbContext _context;

    public VolunteerController(ISender mediator, ApplicationDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    // GET: api/v1/Volunteer
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Get([FromQuery] int page = 1, int pageSize = 20)
    {
        var paginationParams = new PaginationParams
        {
            Page = page,
            PageSize = pageSize
        };
        return Ok(await _mediator.Send(new GetAllVolunteersQuery { PaginationParams = paginationParams }));
    }

    // GET: api/v1/Volunteer/5
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Get(int id)
    {
        return Ok(await _mediator.Send(new GetVolunteerByIdQuery { Id = id }));
    }

    // GET: api/v1/Volunteer/user/some-user-id
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin,Data Contributor")]
    public async Task<IActionResult> GetByUserId(string userId)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!User.IsInRole("Admin") && User.IsInRole("Data Contributor") && !string.Equals(currentUserId, userId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        return Ok(await _mediator.Send(new GetVolunteerByUserIdQuery { UserId = userId }));
    }

    // POST: api/v1/Volunteer/setup
    [HttpPost("setup")]
    [Authorize(Roles = "Data Contributor")]
    public async Task<IActionResult> SetupData([FromBody] Application.Features.Volunteers.Commands.SetupVolunteerData.SetupVolunteerDataCommand command)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        command.UserId = userId; // Force the command to use the authenticated user's ID

        return Ok(await _mediator.Send(command));
    }

    // POST: api/v1/Volunteer
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Post(CreateVolunteerCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    // PUT: api/v1/Volunteer/5/status
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

    // POST: api/v1/Volunteer/5/upload
    [HttpPost("{id}/upload")]
    [Authorize(Roles = "Data Contributor")]
    [EnableRateLimiting("upload")]
    // Allow the request to reach the controller so we can return a clear JSON validation message.
    // Business rule remains 50MB max (enforced below).
    [RequestSizeLimit(62914560)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFile(int id)
    {
        var file = Request.Form.Files[0];
        
        // Validate file is not null and has content
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        // Web upload: JSON only (YouTube comments export)
        if (string.IsNullOrEmpty(file.FileName) ||
            !file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only JSON files are accepted." });
        }

        // Validate file size (50MB max)
        const long maxFileSize = 50 * 1024 * 1024; // 50MB
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { message = "File size must be under 50MB." });
        }

        // Verify the authenticated user owns this volunteer record
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

        // Validate JSON content early for clear UX/security (reject disguised or malformed JSON).
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

    // PUT: api/v1/Volunteer/push-token
    [HttpPut("push-token")]
    [Authorize(Roles = "Data Contributor")]
    public async Task<IActionResult> UpdatePushToken([FromBody] UpdatePushTokenRequest request)
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

    // POST: api/v1/Volunteer/upload-file (for mobile app)
    [HttpPost("upload-file")]
    [Authorize(Roles = "Data Contributor")]
    [EnableRateLimiting("upload")]
    // Allow the request to reach the controller so we can return a clear JSON validation message.
    // Business rule remains 50MB max (enforced below).
    [RequestSizeLimit(62914560)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFileMobile()
    {
        var file = Request.Form.Files[0];
        
        // Validate file is not null and has content
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        // Mobile upload: ZIP only (Google Takeout)
        if (string.IsNullOrEmpty(file.FileName) ||
            !file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only ZIP files are accepted." });
        }

        // Validate file size (50MB max)
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

    // DELETE: api/v1/Volunteer/5/my-data (GDPR right to erasure)
    [HttpDelete("{id}/my-data")]
    [Authorize]
    public async Task<IActionResult> DeleteMyData(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        // Verify the authenticated user owns this volunteer record
        var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.Id == id);
        if (volunteer == null)
        {
            return NotFound(new { message = "Data Contributor not found" });
        }
        if (volunteer.UserId != userId)
        {
            return Forbid();
        }

        var command = new RequestDataDeletionCommand
        {
            VolunteerId = id,
            RequestedByUserId = userId
        };

        var result = await _mediator.Send(command);

        return Ok(result);
    }

    // GET: api/v1/Volunteer/5/export-my-data (GDPR right to portability)
    // DO NOT include ContributorEmails in this response.
    [HttpGet("{id}/export-my-data")]
    [Authorize]
    public async Task<IActionResult> ExportMyData(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        // Verify the authenticated user owns this volunteer record
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

        var result = await _mediator.Send(query);

        return Ok(result);
    }

    // GET: api/v1/Volunteer/estimate-value
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

        var result = await _mediator.Send(command);

        return Ok(result);
    }

    // POST: api/v1/Volunteer/calculate-my-value
    [HttpPost("calculate-my-value")]
    [Authorize]
    public async Task<IActionResult> CalculateMyValue([FromBody] CalculateMyValueRequest request)
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

        var result = await _mediator.Send(command);

        return Ok(result);
    }
}

public class UpdatePushTokenRequest
{
    public string? PushToken { get; set; }
}

public class CalculateMyValueRequest
{
    public int CommentCountEstimate { get; set; }
    public string? ContentTypes { get; set; }
    public string? YouTubeAccountAge { get; set; }
}
