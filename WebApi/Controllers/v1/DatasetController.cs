using System.Text.Json;
using Application.Common;
using Application.Features.Datasets.Commands.AnalyzeDataset;
using Application.Features.Datasets.Commands.BuildDataset;
using Application.Features.Datasets.Commands.CreateDataset;
using Application.Features.Datasets.Commands.UpdateDataset;
using Application.Features.Datasets.DTOs;
using Application.Features.Datasets.Queries.GetAllDatasets;
using Application.Features.Datasets.Queries.GetDatasetById;
using Application.Features.Reviews.Commands.CreateReview;
using Application.Features.Reviews.Queries.GetDatasetReviews;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[EnableRateLimiting("api")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DatasetController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IRepositoryAsync<DatasetPurchase> _purchaseRepository;
    private readonly IDatasetRepository _datasetRepository;
    private readonly IWebHostEnvironment _environment;

    public DatasetController(ISender mediator, IRepositoryAsync<DatasetPurchase> purchaseRepository, IDatasetRepository datasetRepository, IWebHostEnvironment environment)
    {
        _mediator = mediator;
        _purchaseRepository = purchaseRepository;
        _datasetRepository = datasetRepository;
        _environment = environment;
    }

    // GET: api/v1/Dataset
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] int page = 1, int pageSize = 20,
        [FromQuery] string? language = null, [FromQuery] string? contentCategory = null,
        [FromQuery] int? minCommentCount = null, [FromQuery] decimal? maxPrice = null)
    {
        var paginationParams = new PaginationParams
        {
            Page = page,
            PageSize = pageSize
        };
        return Ok(await _mediator.Send(new GetAllDatasetsQuery
        {
            PaginationParams = paginationParams,
            Language = language,
            ContentCategory = contentCategory,
            MinCommentCount = minCommentCount,
            MaxPrice = maxPrice
        }));
    }

    // GET: api/v1/Dataset/5
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(int id)
    {
        return Ok(await _mediator.Send(new GetDatasetByIdQuery { Id = id }));
    }

    // POST: api/v1/Dataset
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Post(CreateDatasetCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    // PUT: api/v1/Dataset/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Put(int id, UpdateDatasetCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest();
        }

        return Ok(await _mediator.Send(command));
    }

    // POST: api/v1/Dataset/build
    [HttpPost("build")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Build(BuildDatasetCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    // GET: api/v1/Dataset/{id}/download?token={token}
    [HttpGet("{id}/download")]
    [Authorize(Roles = "Data Client,Admin")]
    public async Task<IActionResult> Download(Guid id, Guid token)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        var isDataClient = User.IsInRole("Data Client");

        if (!isAdmin && !isDataClient)
        {
            // Should be unreachable due to [Authorize(Roles=...)] but keep it explicit.
            return Forbid();
        }

        Guid? buyerId = null;
        if (!isAdmin)
        {
            if (!Guid.TryParse(currentUserId, out var parsedBuyerId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            buyerId = parsedBuyerId;
        }

        // Find a valid purchase with this token. For buyers, enforce ownership.
        var purchases = await _purchaseRepository.ListAsync();
        var validPurchase = purchases
            .Where(p => p.DatasetId == id && p.DownloadExpiry > DateTime.UtcNow && !string.IsNullOrEmpty(p.DownloadUrl))
            .FirstOrDefault(p =>
            {
                if (!TryExtractDownloadToken(p.DownloadUrl, out var urlToken))
                    return false;

                if (urlToken != token)
                    return false;

                if (buyerId.HasValue && p.BuyerId != buyerId.Value)
                    return false;

                return true;
            });

        if (validPurchase == null)
        {
            return NotFound(new { message = "Invalid or expired download token" });
        }

        // Get the file path
        var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", "datasets", $"{id}.csv");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "File not found" });
        }

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return File(fileStream, "text/csv", $"{id}.csv");
    }

    // POST: api/v1/Dataset/{id}/analyze
    [HttpPost("{id}/analyze")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Analyze(int id)
    {
        return Ok(await _mediator.Send(new AnalyzeDatasetCommand { DatasetId = id }));
    }

    // GET: api/v1/Dataset/{id}/analysis
    [HttpGet("{id}/analysis")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAnalysis(int id)
    {
        var dataset = await _datasetRepository.GetByIdAsync(id);
        if (dataset == null)
        {
            return NotFound(new ServiceResponse<DatasetAnalysisDto>("Dataset not found"));
        }

        if (string.IsNullOrEmpty(dataset.AnalysisResult))
        {
            return Ok(new ServiceResponse<DatasetAnalysisDto>(new DatasetAnalysisDto
            {
                Summary = "Analysis pending. Admin can trigger analysis using POST /api/v1/Dataset/{id}/analyze",
                AnalyzedAt = DateTime.UtcNow,
                CommentCount = 0
            }));
        }

        var analysisResult = JsonSerializer.Deserialize<DatasetAnalysisDto>(dataset.AnalysisResult);
        return Ok(new ServiceResponse<DatasetAnalysisDto>(analysisResult));
    }

    // POST: api/v1/Dataset/{id}/review
    [HttpPost("{id}/review")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> CreateReview(int id, CreateReviewCommand command)
    {
        if (id != command.DatasetId)
        {
            return BadRequest(new ServiceResponse<string>("Dataset ID mismatch"));
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            command.BuyerId = Guid.Parse(userId);
        }

        return Ok(await _mediator.Send(command));
    }

    // GET: api/v1/Dataset/{id}/reviews
    [HttpGet("{id}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews(int id)
    {
        return Ok(await _mediator.Send(new GetDatasetReviewsQuery { DatasetId = id }));
    }

    // GET: api/v1/Dataset/{id}/stream
    [HttpGet("{id}/stream")]
    [Authorize(Roles = "Data Client")]
    public async Task<IActionResult> Stream(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Verify the buyer has a valid, non-refunded purchase for this dataset
        var purchases = await _purchaseRepository.ListAsync();
        var validPurchase = purchases.FirstOrDefault(p =>
            p.DatasetId == id &&
            p.BuyerId == Guid.Parse(userId) &&
            !p.IsRefunded);

        if (validPurchase == null)
        {
            return Forbid();
        }

        // Read the dataset CSV from wwwroot/datasets/
        var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", "datasets", $"{id}.csv");

        if (!System.IO.File.Exists(filePath))
        {
            return StatusCode(403, new ServiceResponse<string>("Dataset file not found"));
        }

        var csvContent = await System.IO.File.ReadAllTextAsync(filePath);
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
        {
            return Ok(new ServiceResponse<List<Dictionary<string, string>>>(new List<Dictionary<string, string>>()));
        }

        // Parse CSV header
        var headers = ParseCsvLine(lines[0]);
        var records = new List<Dictionary<string, string>>();

        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            var record = new Dictionary<string, string>();

            for (int j = 0; j < headers.Count && j < values.Count; j++)
            {
                record[headers[j]] = values[j];
            }

            records.Add(record);
        }

        return Ok(new ServiceResponse<List<Dictionary<string, string>>>(records));
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString().Trim());
        return result;
    }

    private static bool TryExtractDownloadToken(string url, out Guid token)
    {
        token = default;

        if (string.IsNullOrWhiteSpace(url))
            return false;

        // Accept absolute or relative URLs. We only care about the query string.
        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            return false;

        var query = uri.IsAbsoluteUri
            ? uri.Query
            : (url.Contains('?') ? url[url.IndexOf('?')..] : string.Empty);

        if (string.IsNullOrWhiteSpace(query))
            return false;

        if (query.StartsWith("?", StringComparison.Ordinal))
            query = query[1..];

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2)
                continue;

            if (!string.Equals(kv[0], "token", StringComparison.OrdinalIgnoreCase))
                continue;

            var raw = Uri.UnescapeDataString(kv[1]);
            return Guid.TryParse(raw, out token);
        }

        return false;
    }
}
