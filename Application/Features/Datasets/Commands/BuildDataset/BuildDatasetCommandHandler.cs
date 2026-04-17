using System.Text.Json;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Datasets.Commands.BuildDataset;

public class BuildDatasetCommandHandler : IRequestHandler<BuildDatasetCommand, ServiceResponse<BuildDatasetResult>>
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ISpotifyRecordRepository _spotifyRecordRepository;
    private readonly INetflixRecordRepository _netflixRecordRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDatasetSubscriptionRepository _subscriptionRepository;
    private readonly IEmailService _emailService;

    public BuildDatasetCommandHandler(
        IDatasetRepository datasetRepository,
        IFileStorageService fileStorageService,
        ISpotifyRecordRepository spotifyRecordRepository,
        INetflixRecordRepository netflixRecordRepository,
        IAuditLogService auditLogService,
        IDatasetSubscriptionRepository subscriptionRepository,
        IEmailService emailService)
    {
        _datasetRepository = datasetRepository;
        _fileStorageService = fileStorageService;
        _spotifyRecordRepository = spotifyRecordRepository;
        _netflixRecordRepository = netflixRecordRepository;
        _auditLogService = auditLogService;
        _subscriptionRepository = subscriptionRepository;
        _emailService = emailService;
    }

    public async Task<ServiceResponse<BuildDatasetResult>> Handle(BuildDatasetCommand request, CancellationToken cancellationToken)
    {
        string csvContent;
        int recordCount;

        if (request.DataSourceType == DataSourceType.Spotify)
        {
            (csvContent, recordCount) = await BuildSpotifyDatasetAsync(request.VolunteerIds);
        }
        else if (request.DataSourceType == DataSourceType.Netflix)
        {
            (csvContent, recordCount) = await BuildNetflixDatasetAsync(request.VolunteerIds);
        }
        else if (request.DataSourceType == DataSourceType.Combined)
        {
            (csvContent, recordCount) = await BuildCombinedDatasetAsync(request.VolunteerIds);
        }
        else
        {
            // Default to YouTube
            (csvContent, recordCount) = await BuildYouTubeDatasetAsync(request.VolunteerIds);
        }

        if (recordCount == 0)
        {
            return new ServiceResponse<BuildDatasetResult>("No anonymized records found for the specified volunteers");
        }

        var dataset = new Dataset
        {
            Title = request.DatasetName,
            Description = string.Empty,
            VolunteerCount = request.VolunteerIds.Count,
            CommentCount = recordCount,
            Price = request.Price,
            Status = "Active",
            BuyerReference = null
        };

        // Integrity propagation: if any volunteer flagged, dataset flagged. Otherwise verified.
        dataset.IntegrityStatus = IntegrityStatus.Pending;
        var volunteerHashes = new List<string>();

        try
        {
            var volunteerRepo = _datasetRepository.GetVolunteerRepository();
            if (volunteerRepo != null)
            {
                var volunteers = await volunteerRepo.GetByIdsAsync(request.VolunteerIds);
                var flagged = volunteers.Where(v => v.IntegrityStatus == IntegrityStatus.Flagged).ToList();
                volunteerHashes = volunteers
                    .Where(v => !string.IsNullOrWhiteSpace(v.DataIntegrityHash))
                    .Select(v => v.DataIntegrityHash!)
                    .OrderBy(h => h)
                    .ToList();

                if (flagged.Any())
                {
                    dataset.IntegrityStatus = IntegrityStatus.Flagged;
                    dataset.IntegrityReason = "One or more contributors flagged for hash mismatch";
                }
                else if (volunteerHashes.Any())
                {
                    dataset.IntegrityStatus = IntegrityStatus.Verified;
                }
                else
                {
                    dataset.IntegrityStatus = IntegrityStatus.Pending;
                }
            }
        }
        catch
        {
            dataset.IntegrityStatus = IntegrityStatus.Pending;
        }

        await _datasetRepository.AddAsync(dataset);

        var downloadUrl = await _fileStorageService.SaveDatasetCsv(dataset.Id.ToString(), csvContent);

        // Compute combined integrity hash for dataset if we have contributor hashes
        if (volunteerHashes.Any())
        {
            var combined = string.Join("|", volunteerHashes);
            dataset.DataIntegrityHash = ComputeSha256Hex(combined);
            await _datasetRepository.UpdateAsync(dataset);
        }

        // Generate and save provenance document
        var provenance = new ProvenanceDocument
        {
            DatasetId = dataset.Id,
            BuiltAt = DateTime.UtcNow,
            VolunteerCount = dataset.VolunteerCount,
            CommentCount = recordCount,
            ConsentRecordsAvailable = true,
            AnonymizationMethod = "K-anonymity with field suppression",
            PipelineVersion = "1.0.0",
            DataCollectionMethod = "Voluntary Google Takeout export"
        };
        var provenanceJson = JsonSerializer.Serialize(provenance, new JsonSerializerOptions { WriteIndented = true });
        var provenanceUrl = await _fileStorageService.SaveProvenanceJson(dataset.Id.ToString(), provenanceJson);

        var result = new BuildDatasetResult
        {
            DatasetId = dataset.Id,
            DownloadUrl = downloadUrl
        };

        // Notify active subscribers about the updated dataset
        try
        {
            var activeSubscriptions = await _subscriptionRepository.GetActiveByDatasetIdAsync(dataset.Id);
            foreach (var sub in activeSubscriptions)
            {
                // Update next delivery date based on pricing model
                sub.NextDeliveryDate = sub.PricingModel switch
                {
                    "Monthly" => DateTime.UtcNow.AddMonths(1),
                    "Quarterly" => DateTime.UtcNow.AddMonths(3),
                    _ => DateTime.UtcNow.AddMonths(1)
                };
                await _subscriptionRepository.UpdateAsync(sub);
            }
        }
        catch
        {
            // Subscriber notification is non-critical; do not fail the build
        }

        // Audit logging for dataset build
        await _auditLogService.LogAsync(
            action: "DatasetBuilt",
            entityType: "Dataset",
            entityId: dataset.Id.ToString(),
            success: true,
            newValues: "{\"VolunteerCount\":" + dataset.VolunteerCount + ",\"CommentCount\":" + dataset.CommentCount + ",\"DataSourceType\":\"" + request.DataSourceType + "\"}");

        return new ServiceResponse<BuildDatasetResult>(result);
    }

    private static string ComputeSha256Hex(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private async Task<(string csvContent, int recordCount)> BuildYouTubeDatasetAsync(List<Guid> volunteerIds)
    {
        var comments = await _datasetRepository.GetAnonymizedCommentsByVolunteerIds(volunteerIds);

        if (!comments.Any())
        {
            return (string.Empty, 0);
        }

        var uniqueVolunteerIds = comments.Select(c => c.VolunteerId).Distinct().ToList();
        
        var volunteerMapping = new Dictionary<int, string>();
        for (int i = 0; i < uniqueVolunteerIds.Count; i++)
        {
            volunteerMapping[uniqueVolunteerIds[i]] = $"USER_{i + 1:D3}";
        }

        var csvLines = new List<string>
        {
            "user_id,comment_text,timestamp,video_id,like_count,consent_verified,anonymization_method"
        };

        foreach (var comment in comments)
        {
            var userId = volunteerMapping[comment.VolunteerId];
            var commentText = EscapeCsvField(comment.CommentText);
            var timestamp = comment.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            var videoId = comment.VideoId;
            var likeCount = comment.LikeCount;
            var consentVerified = comment.IsAnonymized ? "true" : "false";
            var anonymizationMethod = comment.AnonymizationMethod ?? "manual";

            csvLines.Add($"{userId},{commentText},{timestamp},{videoId},{likeCount},{consentVerified},{anonymizationMethod}");
        }

        return (string.Join(Environment.NewLine, csvLines), comments.Count);
    }

    private async Task<(string csvContent, int recordCount)> BuildSpotifyDatasetAsync(List<Guid> volunteerIds)
    {
        var allRecords = await _spotifyRecordRepository.GetAnonymizedByUserIdsAsync(volunteerIds);

        if (!allRecords.Any())
        {
            return (string.Empty, 0);
        }

        var uniqueVolunteerIds = allRecords.Select(r => r.VolunteerId).Distinct().ToList();
        
        var volunteerMapping = new Dictionary<int, string>();
        for (int i = 0; i < uniqueVolunteerIds.Count; i++)
        {
            volunteerMapping[uniqueVolunteerIds[i]] = $"USER_{i + 1:D3}";
        }

        var csvLines = new List<string>
        {
            "user_id,track_name,artist_name,album_name,played_at,ms_played,platform,consent_verified"
        };

        foreach (var record in allRecords)
        {
            var userId = volunteerMapping[record.VolunteerId];
            var trackName = EscapeCsvField(record.TrackName);
            var artistName = EscapeCsvField(record.ArtistName);
            var albumName = EscapeCsvField(record.AlbumName);
            var playedAt = record.PlayedAt.ToString("yyyy-MM-dd HH:mm:ss");
            var msPlayed = record.MsPlayed;
            var platform = record.Platform;
            var consentVerified = record.IsAnonymized ? "true" : "false";

            csvLines.Add($"{userId},{trackName},{artistName},{albumName},{playedAt},{msPlayed},{platform},{consentVerified}");
        }

        return (string.Join(Environment.NewLine, csvLines), allRecords.Count);
    }

    private async Task<(string csvContent, int recordCount)> BuildNetflixDatasetAsync(List<Guid> volunteerIds)
    {
        var allRecords = await _netflixRecordRepository.GetAnonymizedByUserIdsAsync(volunteerIds);

        if (!allRecords.Any())
        {
            return (string.Empty, 0);
        }

        var uniqueVolunteerIds = allRecords.Select(r => r.VolunteerId).Distinct().ToList();
        
        var volunteerMapping = new Dictionary<int, string>();
        for (int i = 0; i < uniqueVolunteerIds.Count; i++)
        {
            volunteerMapping[uniqueVolunteerIds[i]] = $"USER_{i + 1:D3}";
        }

        var csvLines = new List<string>
        {
            "user_id,title,show_title,watched_date,duration_minutes,device_type,country,consent_verified"
        };

        foreach (var record in allRecords)
        {
            var userId = volunteerMapping[record.VolunteerId];
            var title = EscapeCsvField(record.Title);
            var showTitle = EscapeCsvField(record.ShowTitle);
            var watchedDate = record.WatchedDate.ToString("yyyy-MM-dd HH:mm:ss");
            var durationMinutes = record.DurationMinutes;
            var deviceType = record.DeviceType;
            var country = record.Country;
            var consentVerified = record.IsAnonymized ? "true" : "false";

            csvLines.Add($"{userId},{title},{showTitle},{watchedDate},{durationMinutes},{deviceType},{country},{consentVerified}");
        }

        return (string.Join(Environment.NewLine, csvLines), allRecords.Count);
    }

    private async Task<(string csvContent, int recordCount)> BuildCombinedDatasetAsync(List<Guid> volunteerIds)
    {
        var youtubeResult = await BuildYouTubeDatasetAsync(volunteerIds);
        var spotifyResult = await BuildSpotifyDatasetAsync(volunteerIds);
        var netflixResult = await BuildNetflixDatasetAsync(volunteerIds);

        if (youtubeResult.recordCount == 0 && spotifyResult.recordCount == 0 && netflixResult.recordCount == 0)
        {
            return (string.Empty, 0);
        }

        var sections = new List<string>();

        if (youtubeResult.recordCount > 0)
        {
            sections.Add("# YOUTUBE COMMENTS");
            sections.Add(youtubeResult.csvContent);
        }

        if (spotifyResult.recordCount > 0)
        {
            if (sections.Any())
            {
                sections.Add("");
            }
            sections.Add("# SPOTIFY LISTENING HISTORY");
            sections.Add(spotifyResult.csvContent);
        }

        if (netflixResult.recordCount > 0)
        {
            if (sections.Any())
            {
                sections.Add("");
            }
            sections.Add("# NETFLIX VIEWING HISTORY");
            sections.Add(netflixResult.csvContent);
        }

        return (string.Join(Environment.NewLine, sections), youtubeResult.recordCount + spotifyResult.recordCount + netflixResult.recordCount);
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
