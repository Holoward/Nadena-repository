using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Context;

namespace Persistence.Services;

/// <summary>
/// Writes anonymised WatchEvent records from the database into a CSV file under
/// wwwroot/datasets/{datasetId}.csv so buyers can download real, purchased data.
/// NOTE: For multi-instance deployments, replace with a cloud storage implementation
/// (S3 / Azure Blob) before horizontal scaling.
/// </summary>
public class LocalDatasetStorageService : IDatasetStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalDatasetStorageService> _logger;
    private readonly ApplicationDbContext _dbContext;

    public LocalDatasetStorageService(
        IWebHostEnvironment environment,
        ILogger<LocalDatasetStorageService> logger,
        ApplicationDbContext dbContext)
    {
        _environment = environment;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task WriteDatasetFileAsync(
        Guid datasetId,
        IEnumerable<string> dataSources,
        string category,
        DateTime? dateRangeStart,
        DateTime? dateRangeEnd)
    {
        _logger.LogWarning(
            "Using LocalDatasetStorageService. This will break horizontally scaled " +
            "deployments. Migrate to cloud storage (S3/Azure Blob) before scaling.");

        var directory = Path.Combine(_environment.WebRootPath ?? "wwwroot", "datasets");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, $"{datasetId}.csv");

        // Build the query against the real WatchEvents table.
        var query = _dbContext.WatchEvents.AsNoTracking().AsQueryable();

        // Filter by category when the buyer requests a specific one.
        if (!string.IsNullOrWhiteSpace(category) &&
            !string.Equals(category, "General", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(e => e.Category == category);
        }

        // Filter by date range when specified.
        if (dateRangeStart.HasValue)
            query = query.Where(e => e.WatchedAt >= dateRangeStart.Value);
        if (dateRangeEnd.HasValue)
            query = query.Where(e => e.WatchedAt <= dateRangeEnd.Value);

        // Order deterministically so repeated requests are consistent.
        var rawEvents = await query
            .OrderBy(e => e.WatchedAt)
            .Select(e => new
            {
                e.ContributorId,
                e.VideoIdHash,
                e.ChannelIdHash,
                e.Category,
                e.WatchedAt,
                e.HourOfDay,
                e.DayOfWeek,
                e.Month,
                e.Year,
                e.IsRepeat,
                e.SessionId,
                e.PositionInSession
            })
            .ToListAsync();

        // Pseudonymize ContributorId per-dataset so buyers cannot correlate contributors
        // across purchases. Uses HMAC-SHA256(contributorId, datasetId) as a stable but
        // unlinkable token scoped to this specific dataset export.
        var datasetSalt = Encoding.UTF8.GetBytes(datasetId.ToString());
        var events = rawEvents.Select(e => new
        {
            PseudoContributorId = Convert.ToHexString(
                HMACSHA256.HashData(datasetSalt, Encoding.UTF8.GetBytes(e.ContributorId.ToString()))
            ).ToLowerInvariant(),
            e.VideoIdHash,
            e.ChannelIdHash,
            e.Category,
            e.WatchedAt,
            e.HourOfDay,
            e.DayOfWeek,
            e.Month,
            e.Year,
            e.IsRepeat,
            e.SessionId,
            e.PositionInSession
        }).ToList();

        if (events.Count == 0)
        {
            _logger.LogWarning(
                "Dataset {DatasetId}: no WatchEvents matched the requested filters " +
                "(category={Category}, start={Start}, end={End}). Writing empty CSV.",
                datasetId, category, dateRangeStart, dateRangeEnd);
        }

        // Write the CSV with proper quoting. No PII is included — ContributorId is a
        // random Guid and VideoIdHash / ChannelIdHash are SHA-256 digests.
        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);

        // Header
        await writer.WriteLineAsync(
            "record_id,contributor_id,video_id_hash,channel_id_hash,category," +
            "watched_at,hour_of_day,day_of_week,month,year,is_repeat,session_id,position_in_session");

        int recordId = 1;
        foreach (var e in events)
        {
            var line = string.Concat(
                recordId++, ",",
                e.PseudoContributorId, ",",
                QuoteCsv(e.VideoIdHash), ",",
                QuoteCsv(e.ChannelIdHash), ",",
                QuoteCsv(e.Category), ",",
                e.WatchedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"), ",",
                e.HourOfDay, ",",
                e.DayOfWeek, ",",
                e.Month, ",",
                e.Year, ",",
                e.IsRepeat ? "true" : "false", ",",
                e.SessionId, ",",
                e.PositionInSession
            );
            await writer.WriteLineAsync(line);
        }

        _logger.LogInformation(
            "Dataset {DatasetId}: wrote {Count} records to {Path}.",
            datasetId, events.Count, filePath);
    }

    /// <summary>
    /// Wraps a value in double-quotes and escapes any internal double-quotes by
    /// doubling them (standard RFC 4180 CSV quoting).
    /// </summary>
    private static string QuoteCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
