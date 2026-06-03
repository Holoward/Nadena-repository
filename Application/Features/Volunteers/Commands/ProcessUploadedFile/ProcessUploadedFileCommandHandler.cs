using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Interfaces;
using Application.Wrappers;
using Application.Exceptions;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Application.Features.Volunteers.Commands.ProcessUploadedFile;

public class ProcessUploadedFileCommandHandler : IRequestHandler<ProcessUploadedFileCommand, ServiceResponse<ProcessUploadedFileResult>>
{
    private readonly IVolunteerRepository _volunteerRepository;
    private readonly ISpotifyRecordRepository _spotifyRecordRepository;
    private readonly INetflixRecordRepository _netflixRecordRepository;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuditLogService _auditLogService;
    private string _lastHashComputed = string.Empty;

    // Security constants
    private const long MaxFileSizeBytes = 500L * 1024 * 1024; // 500MB
    private const int MaxEntryCount = 10000;
    private const double MaxCompressionRatio = 100.0;
    private static readonly byte[] ZipMagicBytes = { 0x50, 0x4B, 0x03, 0x04 }; // PK\x03\x04

    public ProcessUploadedFileCommandHandler(
        IVolunteerRepository volunteerRepository,
        ISpotifyRecordRepository spotifyRecordRepository,
        INetflixRecordRepository netflixRecordRepository,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IAuditLogService auditLogService)
    {
        _volunteerRepository = volunteerRepository;
        _spotifyRecordRepository = spotifyRecordRepository;
        _netflixRecordRepository = netflixRecordRepository;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _auditLogService = auditLogService;
    }

    public async Task<ServiceResponse<ProcessUploadedFileResult>> Handle(ProcessUploadedFileCommand request, CancellationToken cancellationToken)
    {
        // SECURITY CHECK 1: File size limit
        if (request.FileSize > MaxFileSizeBytes)
        {
            return new ServiceResponse<ProcessUploadedFileResult>("File exceeds maximum allowed size of 500MB");
        }

        var volunteer = await _volunteerRepository.GetByIdAsync(request.VolunteerId);
        if (volunteer == null)
            throw new ApiException($"Data Contributor not found with Id {request.VolunteerId}");

        // Buffer stream once so we can hash and parse consistently
        using var buffer = new MemoryStream();
        await request.FileStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var rawHash = ComputeSha256Hex(buffer);
        _lastHashComputed = rawHash;

        buffer.Position = 0; // reset for downstream processing

        // SECURITY CHECK 2: File type check - verify ZIP magic bytes or JSON
        bool isZip = IsValidZipFile(buffer);
        buffer.Position = 0;

        if (!isZip && !request.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return new ServiceResponse<ProcessUploadedFileResult>("Invalid file format. Only ZIP or JSON files are allowed.");
        }

        int recordCount = 0;
        string? warningMessage = null;
        bool isSpotifyZip = false;
        bool isNetflixZip = false;

        if (isZip)
        {
            using var zipArchive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: true);

            // SECURITY CHECK 3: ZIP bomb protection - check compression ratio
            // SECURITY CHECK 4: Entry count limit
            var (totalCompressedSize, totalUncompressedSize, entryCount) = AnalyzeZipEntries(zipArchive);
            
            if (entryCount > MaxEntryCount)
            {
                return new ServiceResponse<ProcessUploadedFileResult>("ZIP contains too many files. Maximum allowed is 10000.");
            }

            if (totalCompressedSize > 0)
            {
                var compressionRatio = (double)totalUncompressedSize / totalCompressedSize;
                if (compressionRatio > MaxCompressionRatio)
                {
                    return new ServiceResponse<ProcessUploadedFileResult>("Suspicious compression ratio detected. File may be a ZIP bomb.");
                }
            }

            // SECURITY CHECK 6: Allowed entry names - filter to Google Takeout paths
            var validEntries = FilterValidEntries(zipArchive.Entries.ToList());

            // Detect ZIP type by checking contents
            var detected = DetectZipType(zipArchive);
            isSpotifyZip = detected.isSpotify;
            isNetflixZip = detected.isNetflix;

            if (isSpotifyZip)
            {
                recordCount = await ProcessSpotifyZipAsync(zipArchive, volunteer.Id);
                volunteer.DataSourceType = DataSourceType.Spotify;
            }
            else if (isNetflixZip)
            {
                recordCount = await ProcessNetflixZipAsync(zipArchive, volunteer.Id);
                volunteer.DataSourceType = DataSourceType.Netflix;
            }
            else
            {
                // YouTube comment processing removed — behavioral data now collected via TakeoutController
                volunteer.DataSourceType = DataSourceType.YouTube;
            }
        }
        else
        {
            // YouTube comment processing removed — behavioral data now collected via TakeoutController
            volunteer.DataSourceType = DataSourceType.YouTube;
        }

        // Integrity tracking
        if (!string.IsNullOrWhiteSpace(volunteer.DataIntegrityHash) && !string.Equals(volunteer.DataIntegrityHash, rawHash, StringComparison.Ordinal))
        {
            volunteer.IntegrityStatus = IntegrityStatus.Flagged;
            volunteer.IntegrityReason = "Hash mismatch on re-upload";
        }
        else
        {
            volunteer.IntegrityStatus = IntegrityStatus.Verified;
            volunteer.IntegrityReason = null;
        }
        volunteer.DataIntegrityHash = rawHash;

        // Update volunteer status to FileReceived
        volunteer.Status = VolunteerStatus.FileReceived;
        volunteer.FileLink = $"/api/v1/Volunteer/{volunteer.Id}/file";
        await _volunteerRepository.UpdateAsync(volunteer);

        // Trigger webhook (non-blocking)
        try
        {
            var webhookUrl = _configuration["NadenaSettings:FileUploadedWebhook"];
            if (!string.IsNullOrEmpty(webhookUrl))
            {
                var httpClient = _httpClientFactory.CreateClient();
                var payload = new
                {
                    volunteerId = volunteer.Id.ToString(),
                    fileLink = volunteer.FileLink,
                    recordCount = recordCount,
                    dataSourceType = volunteer.DataSourceType.ToString(),
                    uploadedAt = DateTime.UtcNow
                };
                await httpClient.PostAsJsonAsync(webhookUrl, payload);
            }
        }
        catch
        {
            // Non-blocking - don't fail the upload if webhook fails
        }

        var resultDto = new ProcessUploadedFileResult
        {
            TotalCommentsProcessed = recordCount,
            WarningMessage = warningMessage,
            IntegrityHash = rawHash,
            IntegrityStatus = volunteer.IntegrityStatus.ToString(),
            IntegrityReason = volunteer.IntegrityReason
        };

        // Audit logging for successful file upload
        var sourceType = isSpotifyZip ? "Spotify" : (isNetflixZip ? "Netflix" : "YouTube");
        await _auditLogService.LogAsync(
            action: "FileUploaded",
            entityType: "Volunteer",
            entityId: volunteer.Id.ToString(),
            success: true,
            userId: volunteer.UserId,
            newValues: "{\"RecordCount\":" + recordCount + ",\"DataSourceType\":\"" + sourceType + "\"}");

        return new ServiceResponse<ProcessUploadedFileResult>(resultDto, $"Successfully processed {recordCount} {sourceType} records");
    }

    /// <summary>
    /// SECURITY CHECK 2: Verifies the file has correct ZIP magic bytes (PK\x03\x04)
    /// </summary>
    private bool IsValidZipFile(Stream stream)
    {
        var header = new byte[4];
        var bytesRead = stream.Read(header, 0, 4);
        
        if (bytesRead < 4)
            return false;

        for (int i = 0; i < 4; i++)
        {
            if (header[i] != ZipMagicBytes[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// SECURITY CHECK 3 & 4: Analyzes ZIP entries for compression ratio and count
    /// </summary>
    private (long totalCompressed, long totalUncompressed, int entryCount) AnalyzeZipEntries(ZipArchive zipArchive)
    {
        long totalCompressed = 0;
        long totalUncompressed = 0;
        int entryCount = 0;

        foreach (var entry in zipArchive.Entries)
        {
            totalCompressed += entry.CompressedLength;
            totalUncompressed += entry.Length;
            entryCount++;
        }

        return (totalCompressed, totalUncompressed, entryCount);
    }

    private static string ComputeSha256Hex(Stream input)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(input);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// SECURITY CHECK 5 & 6: Path traversal protection and allowed entry names filtering
    /// </summary>
    private List<ZipArchiveEntry> FilterValidEntries(List<ZipArchiveEntry> entries)
    {
        var validEntries = new List<ZipArchiveEntry>();

        // Allowed patterns for Google Takeout
        var allowedPatterns = new[]
        {
            "YouTube", "Takeout", "comments",
            "StreamingHistory", "ViewingActivity"
        };

        foreach (var entry in entries)
        {
            var entryName = entry.FullName;

            // SECURITY CHECK 5: Path traversal protection
            // Skip entries with ".." or starting with "/" (absolute paths)
            if (entryName.Contains("..") || entryName.StartsWith("/"))
            {
                continue;
            }

            // SECURITY CHECK 6: Only process entries matching expected Google Takeout paths
            var matchesAllowedPattern = allowedPatterns.Any(pattern =>
                entryName.Contains(pattern, StringComparison.OrdinalIgnoreCase));

            if (matchesAllowedPattern)
            {
                validEntries.Add(entry);
            }
        }

        return validEntries;
    }

    private (bool isSpotify, bool isNetflix) DetectZipType(ZipArchive zipArchive)
    {
        // Check for Netflix: ViewingActivity.csv
        var netflixFile = zipArchive.Entries
            .Any(e => e.FullName.EndsWith("ViewingActivity.csv", StringComparison.OrdinalIgnoreCase));

        if (netflixFile)
            return (false, true);

        // Check for Spotify: StreamingHistory_music_*.json files
        var spotifyFiles = zipArchive.Entries
            .Any(e => e.FullName.StartsWith("StreamingHistory_music", StringComparison.OrdinalIgnoreCase) 
                   && e.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

        if (spotifyFiles)
            return (true, false);

        // Check for YouTube: Takeout/YouTube folder
        var youtubeFolder = zipArchive.Entries
            .Any(e => e.FullName.StartsWith("Takeout/YouTube", StringComparison.OrdinalIgnoreCase) 
                   || e.FullName.StartsWith("YouTube/", StringComparison.OrdinalIgnoreCase));

        return (!youtubeFolder, false); // Default to YouTube if neither found
    }

    private async Task<int> ProcessSpotifyZipAsync(ZipArchive zipArchive, int volunteerId)
    {
        var spotifyRecords = new List<SpotifyListeningRecord>();

        var streamingHistoryFiles = zipArchive.Entries
            .Where(e => e.FullName.StartsWith("StreamingHistory_music", StringComparison.OrdinalIgnoreCase)
                     && e.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var file in streamingHistoryFiles)
        {
            using var streamReader = new StreamReader(file.Open());
            var jsonContent = await streamReader.ReadToEndAsync();

            var records = JsonSerializer.Deserialize<List<SpotifyStreamingRecord>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (records != null)
            {
                foreach (var record in records)
                {
                    if (record.MsPlayed < 1000)
                        continue;

                    var spotifyRecord = new SpotifyListeningRecord
                    {
                        VolunteerId = volunteerId,
                        TrackName = AnonymizeField(record.MasterMetadataTrackName),
                        ArtistName = AnonymizeField(record.MasterMetadataAlbumArtistName),
                        AlbumName = AnonymizeField(record.MasterMetadataAlbumAlbumName),
                        PlayedAt = ParseSpotifyTimestamp(record.Ts),
                        MsPlayed = record.MsPlayed,
                        Platform = AnonymizeField(record.Platform),
                        IsAnonymized = true
                    };
                    spotifyRecords.Add(spotifyRecord);
                }
            }
        }

        if (spotifyRecords.Any())
        {
            await _spotifyRecordRepository.AddRangeAsync(spotifyRecords);
        }

        return spotifyRecords.Count;
    }

    private async Task<int> ProcessNetflixZipAsync(ZipArchive zipArchive, int volunteerId)
    {
        var netflixRecords = new List<NetflixViewingRecord>();

        // Find ViewingActivity.csv
        var viewingActivityFile = zipArchive.Entries
            .FirstOrDefault(e => e.FullName.EndsWith("ViewingActivity.csv", StringComparison.OrdinalIgnoreCase));

        if (viewingActivityFile == null)
        {
            throw new ApiException("Could not find ViewingActivity.csv in the uploaded ZIP file");
        }

        using var streamReader = new StreamReader(viewingActivityFile.Open());
        var csvContent = await streamReader.ReadToEndAsync();

        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // Skip header row
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            var record = ParseNetflixCsvLine(line);
            if (record != null)
            {
                record.VolunteerId = volunteerId;
                record.IsAnonymized = true;
                netflixRecords.Add(record);
            }
        }

        if (netflixRecords.Any())
        {
            await _netflixRecordRepository.AddRangeAsync(netflixRecords);
        }

        return netflixRecords.Count;
    }

    private NetflixViewingRecord? ParseNetflixCsvLine(string line)
    {
        try
        {
            // Netflix CSV format: Title, Date, Duration, Attributes, Supplemental Video Type, Device Type, Bookmark, Latest Bookmark, Country
            var parts = ParseCsvLine(line);
            if (parts.Count < 9)
                return null;

            var title = parts[0].Trim();
            var dateStr = parts[1].Trim();
            var durationStr = parts[2].Trim();
            var deviceType = parts[5].Trim();
            var country = parts[8].Trim();

            // Parse date
            if (!DateTime.TryParse(dateStr, out var watchedDate))
                return null;

            // Parse duration (HH:MM:SS format)
            var durationMinutes = ParseDurationToMinutes(durationStr);

            // Extract show title by removing season/episode info
            var showTitle = ExtractShowTitle(title);

            return new NetflixViewingRecord
            {
                Title = title,
                ShowTitle = showTitle,
                WatchedDate = watchedDate,
                DurationMinutes = durationMinutes,
                DeviceType = AnonymizeField(deviceType),
                Country = AnonymizeField(country),
                IsAnonymized = false
            };
        }
        catch
        {
            return null;
        }
    }

    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());

        return result;
    }

    private int ParseDurationToMinutes(string duration)
    {
        if (string.IsNullOrEmpty(duration))
            return 0;

        var parts = duration.Split(':');
        try
        {
            if (parts.Length == 3) // HH:MM:SS
            {
                var hours = int.Parse(parts[0]);
                var minutes = int.Parse(parts[1]);
                var seconds = int.Parse(parts[2]);
                return hours * 60 + minutes + (seconds >= 30 ? 1 : 0);
            }
            else if (parts.Length == 2) // MM:SS
            {
                var minutes = int.Parse(parts[0]);
                var seconds = int.Parse(parts[1]);
                return minutes + (seconds >= 30 ? 1 : 0);
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return 0;
    }

    private string ExtractShowTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return string.Empty;

        // Remove "Season X:" pattern
        var showTitle = Regex.Replace(title, @"\s*Season\s+\d+:\s*", ": ", RegexOptions.IgnoreCase);
        
        // Remove "Episode X:" pattern  
        showTitle = Regex.Replace(showTitle, @"\s*Episode\s+\d+:\s*", ": ", RegexOptions.IgnoreCase);

        // If the title still contains season/episode info at the end, remove it
        showTitle = Regex.Replace(showTitle, @":\s*Season\s+\d+\s*Episode\s+\d+$", "", RegexOptions.IgnoreCase);
        showTitle = Regex.Replace(showTitle, @":\s*Episode\s+\d+$", "", RegexOptions.IgnoreCase);

        return showTitle.Trim();
    }

    private DateTime ParseSpotifyTimestamp(string? timestamp)
    {
        if (string.IsNullOrEmpty(timestamp))
            return DateTime.UtcNow;

        if (DateTime.TryParse(timestamp, out var parsed))
            return parsed;

        return DateTime.UtcNow;
    }

    private string AnonymizeField(string? field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        return field;
    }

    // Helper class for deserializing Spotify streaming history
    private class SpotifyStreamingRecord
    {
        public string? Ts { get; set; }
        public string? MasterMetadataTrackName { get; set; }
        public string? MasterMetadataAlbumArtistName { get; set; }
        public string? MasterMetadataAlbumAlbumName { get; set; }
        public int MsPlayed { get; set; }
        public string? Platform { get; set; }
    }
}
