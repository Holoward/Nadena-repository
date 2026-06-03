using Application.Features.Survey.Commands.ActivateSurvey;
using Application.Features.Survey.Commands.CreateSurvey;
using Application.Features.Survey.Commands.SubmitSurveyResponse;
using Application.Features.Survey.DTOs;
using Application.Features.Survey.Queries.ExportSurveyResponses;
using Application.Features.Survey.Queries.GetActiveSurvey;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[EnableRateLimiting("api")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SurveyController : ControllerBase
{
    private readonly ISender _mediator;

    public SurveyController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("admin/survey")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSurvey([FromBody] Application.Features.Survey.Commands.CreateSurvey.CreateSurveyCommand command)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        command.ResearcherId = userId;

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { data = result.Data });
    }

    [HttpPatch("admin/survey/{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivateSurvey(int id)
    {
        var command = new ActivateSurveyCommand { SurveyTemplateId = id };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { data = result.Data });
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveSurvey()
    {
        var query = new GetActiveSurveyQuery();

        var result = await _mediator.Send(query);

        if (result.Data == null)
        {
            return NotFound(new { message = "No active survey found" });
        }

        return Ok(new { data = result.Data });
    }

    [HttpPost("respond")]
    [Authorize(Roles = "Data Contributor")]
    public async Task<IActionResult> SubmitSurveyResponse([FromBody] SubmitSurveyRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var command = new SubmitSurveyResponseCommand
        {
            ContributorId = userId,
            SurveyTemplateId = request.SurveyTemplateId,
            Responses = request.Responses
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            if (result.Message?.Contains("already completed") == true)
            {
                return Conflict(new { message = result.Message });
            }
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { data = true });
    }

    [HttpGet("admin/survey/{id}/export")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportSurveyResponses(int id)
    {
        var query = new ExportSurveyResponsesQuery { SurveyTemplateId = id };

        var result = await _mediator.Send(query);

        if (result.Data == null)
        {
            return NotFound(new { message = result.Message ?? "Survey not found" });
        }

        var csv = new StringBuilder();
        csv.AppendLine("contributor_id,question_text,question_type,response_value,responded_at");

        foreach (var row in result.Data)
        {
            csv.AppendLine($"\"{EscapeCsvValue(row.ContributorId)}\",\"{EscapeCsvValue(row.QuestionText)}\",\"{row.QuestionType}\",\"{EscapeCsvValue(row.ResponseValue)}\",\"{row.RespondedAt:yyyy-MM-ddTHH:mm:ssZ}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"survey_{id}_responses.csv");
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\"", "\"\"");
    }
}