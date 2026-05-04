using Application.Features.DataPools.Commands.CreateDataPool;
using Application.Features.DataPools.Queries.GetAllDataPools;
using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class DataPoolController : ControllerBase
{
    private readonly DataPoolService _dataPoolService;
    private readonly ISender _mediator;
    private readonly IApiKeyService _apiKeyService;
    private readonly IDataLicenseRepository _licenseRepository;
    private readonly IDataPoolRepository _poolRepository;

    public DataPoolController(
        ISender mediator,
        IApiKeyService apiKeyService,
        IDataLicenseRepository licenseRepository,
        IDataPoolRepository poolRepository,
        DataPoolService dataPoolService)
    {
        _mediator = mediator;
        _apiKeyService = apiKeyService;
        _licenseRepository = licenseRepository;
        _poolRepository = poolRepository;
        _dataPoolService = dataPoolService;
    }

    // GET: api/v1/DataPool
    // Public marketplace — no auth required
    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await _mediator.Send(new GetAllDataPoolsQuery { ActiveOnly = true }));

    // GET: api/v1/DataPool/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var pool = await _poolRepository.GetByIdAsync(id);
        if (pool == null || !pool.IsActive)
            return NotFound(new { message = "Pool not found." });
        return Ok(pool);
    }

    // POST: api/v1/DataPool — Admin only
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Post([FromBody] CreateDataPoolCommand command)
        => Ok(await _mediator.Send(command));

    // PUT: api/v1/DataPool/{id} — Admin only
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Put(int id, [FromBody] UpdatePoolRequest request)
    {
        var pool = await _poolRepository.GetByIdAsync(id);
        if (pool == null)
            return NotFound(new { message = "Pool not found." });

        pool.Name = request.Name ?? pool.Name;
        pool.Description = request.Description ?? pool.Description;
        pool.PricePerMonth = request.PricePerMonth ?? pool.PricePerMonth;
        pool.IsActive = request.IsActive ?? pool.IsActive;
        pool.RevenueSharePercent = request.RevenueSharePercent.HasValue
            ? Math.Clamp(request.RevenueSharePercent.Value, 10m, 90m)
            : pool.RevenueSharePercent;

        await _poolRepository.UpdateAsync(pool);
        return Ok(new { message = "Pool updated.", pool.Id });
    }

    /// <summary>
    /// B2B licensed data access endpoint.
    /// Pass the raw API key in the X-Api-Key header.
    /// Returns paginated anonymized records from the pool.
    /// </summary>
    // GET: api/v1/DataPool/{id}/data
    [HttpGet("{id}/data")]
    public async Task<IActionResult> GetLicensedData(
        int id,
        [FromHeader(Name = "X-Api-Key")] string? apiKey,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { message = "Missing X-Api-Key header." });

        // Validate API key
        var validatedKey = await _apiKeyService.ValidateAsync(apiKey);
        if (validatedKey == null)
            return Unauthorized(new { message = "Invalid, expired, or revoked API key." });

        // Verify license covers this specific pool
        var license = await _licenseRepository.GetActiveLicenseByApiKeyIdAsync(validatedKey.Id);
        if (license == null || license.DataPoolId != id)
            return Forbid();

        var pool = await _poolRepository.GetByIdAsync(id);
        if (pool == null)
            return NotFound(new { message = "Pool not found." });

        return Ok(new
        {
            pool = new { pool.Id, pool.Name, pool.Category },
            license = new
            {
                license.LicensedFrom,
                license.LicensedUntil,
                daysRemaining = (int)(license.LicensedUntil - DateTime.UtcNow).TotalDays
            },
            pagination = new { page, pageSize },
            message = "Dataset delivery is handled via configured DeliveryEndpoint. Contact support for direct API access."
        });
    }

    [HttpGet("{id}/preview")]
    public async Task<IActionResult> GetPreview(int id)
    {
        var pool = await _poolRepository.GetByIdAsync(id);
        if (pool == null || !pool.IsActive)
            return NotFound(new { message = "Pool not found." });
        return Ok(new
        {
            pool = new { pool.Id, pool.Name, pool.Category, pool.ApproximateRecordCount },
            message = "Preview available on request. Contact david@nadena.tech for a sample dataset."
        });
    }
}

public class UpdatePoolRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? PricePerMonth { get; set; }
    public decimal? RevenueSharePercent { get; set; }
    public bool? IsActive { get; set; }
}
