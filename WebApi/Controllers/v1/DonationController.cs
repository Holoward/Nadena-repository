using Application.Features.Donation.Commands.CreateDonation;
using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using System.Text.Json;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[EnableRateLimiting("api")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DonationController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ApplicationDbContext _dbContext;

    public DonationController(ISender mediator, ApplicationDbContext dbContext)
    {
        _mediator = mediator;
        _dbContext = dbContext;
    }

    [HttpPost("create")]
    [EnableCors("ChromeExtension")]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateDonationCommand command)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        command.ContributorId = userGuid;

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { donationId = result.Data });
    }

    // GET: api/v1/admin/donations
    // DO NOT include ContributorEmails in this response.
    [HttpGet("admin/donations")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllDonations([FromQuery] string? format)
    {
        var donations = await _dbContext.Donations
            .AsNoTracking()
            .OrderByDescending(d => d.SubmittedAt)
            .ToListAsync();

        if (format?.ToLower() == "csv")
        {
            var csv = new System.Text.StringBuilder();
            
            // Header row
            csv.AppendLine("contributor_id,consent_timestamp,consent_version,age_range,country,primary_language,content_archetype,total_videos_watched,unique_channels,date_range_start,date_range_end,top_category_1,top_category_2,top_category_3,history_span_days,data_quality_score");

            foreach (var d in donations)
            {
                using var doc = JsonDocument.Parse(d.PayloadJson);
                var root = doc.RootElement;

                var totalVideos = root.TryGetProperty("volume", out var vol) && vol.TryGetProperty("totalVideos", out var tv) && tv.TryGetInt64(out var tvv) ? tvv : 0L;
                var uniqueChannels = root.TryGetProperty("volume", out var vol2) && vol2.TryGetProperty("uniqueChannels", out var uc) && uc.TryGetInt64(out var ucv) ? ucv : 0L;
                var totalDaysSpan = root.TryGetProperty("collectionPeriod", out var cp) && cp.TryGetProperty("totalDaysSpan", out var tds) && tds.TryGetInt32(out var tdsv) ? tdsv : 0;

                var earliestStr = root.TryGetProperty("collectionPeriod", out var cp2) && cp2.TryGetProperty("earliest", out var e) && e.TryGetDateTime(out var ed) ? ed.ToString("yyyy-MM-ddTHH:mm:ssZ") : "";
                var latestStr = root.TryGetProperty("collectionPeriod", out var cp3) && cp3.TryGetProperty("latest", out var l) && l.TryGetDateTime(out var ld) ? ld.ToString("yyyy-MM-ddTHH:mm:ssZ") : "";
                var consentTimestampStr = d.SubmittedAt.ToString("yyyy-MM-ddTHH:mm:ssZ");

                string cat1 = "", cat2 = "", cat3 = "";
                if (root.TryGetProperty("content", out var content) && content.TryGetProperty("categoryDistribution", out var catDist))
                {
                    try
                    {
                        var catJson = catDist.GetRawText();
                        if (!string.IsNullOrEmpty(catJson))
                        {
                            using var catDoc = JsonDocument.Parse(catJson);
                            if (catDoc.RootElement.ValueKind == JsonValueKind.Object)
                            {
                                var sorted = catDoc.RootElement.EnumerateObject()
                                    .OrderByDescending(p => int.TryParse(p.Value.ToString(), out var count) ? count : 0)
                                    .Select(p => p.Name)
                                    .ToList();
                                cat1 = sorted.Count > 0 ? EscapeCsvValue(sorted[0]) : "";
                                cat2 = sorted.Count > 1 ? EscapeCsvValue(sorted[1]) : "";
                                cat3 = sorted.Count > 2 ? EscapeCsvValue(sorted[2]) : "";
                            }
                        }
                    }
                    catch { }
                }

                csv.AppendLine($"{EscapeCsvValue(d.ContributorId)},{consentTimestampStr},{EscapeCsvValue(d.ConsentVersion)},,,,{totalVideos},{uniqueChannels},{earliestStr},{latestStr},{cat1},{cat2},{cat3},{totalDaysSpan},");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "naden_contributors.csv");
        }

        // JSON response
        var records = donations.Select(d =>
        {
            using var doc = JsonDocument.Parse(d.PayloadJson);
            var root = doc.RootElement;

            var getLong = (string prop) => root.TryGetProperty(prop, out var p) && p.TryGetInt64(out var v) ? v : 0L;
            var getDouble = (string prop) => root.TryGetProperty(prop, out var p) && p.TryGetDouble(out var v) ? v : 0.0;
            var getString = (string prop) => root.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
            var getInt = (string prop) => root.TryGetProperty(prop, out var p) && p.TryGetInt32(out var v) ? v : 0;

            DateTime earliest = DateTime.MinValue, latest = DateTime.MinValue;
            if (root.TryGetProperty("collectionPeriod", out var cp))
            {
                if (cp.TryGetProperty("earliest", out var e) && e.TryGetDateTime(out var ed))
                    earliest = ed;
                if (cp.TryGetProperty("latest", out var l) && l.TryGetDateTime(out var ld))
                    latest = ld;
            }

            var content = root.TryGetProperty("content", out var c) ? c.ToString() : "{}";

            return new
            {
                d.ContributorId,
                d.ConsentVersion,
                submittedAt = d.SubmittedAt,
                dataSource = getString("dataSource"),
                earliest,
                latest,
                totalDaysSpan = getInt("collectionPeriod"),
                totalVideos = getLong("volume"),
                uniqueVideos = getLong("volume"),
                uniqueChannels = getLong("volume"),
                repeatViewRate = getDouble("volume"),
                categoryDistribution = content,
                topChannelWatchCounts = root.TryGetProperty("content", out var cc) ? cc.ToString() : "{}"
            };
        }).ToList();

        return Ok(new { data = records });
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}