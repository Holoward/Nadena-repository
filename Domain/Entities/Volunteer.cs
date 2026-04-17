using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Volunteer : AuditableBaseEntity
{
    public string UserId { get; set; }
    public VolunteerStatus Status { get; set; }
    public string? YouTubeAccountAge { get; set; }
    public string? CommentCountEstimate { get; set; }
    public string? ContentTypes { get; set; }
    public string? FileLink { get; set; }
    public DateTime? ActivatedDate { get; set; }
    public string? BuyerReference { get; set; }
    public bool PaymentSent { get; set; }
    public string? Notes { get; set; }
    public string? PayPalEmail { get; set; }
    public string? PushToken { get; set; }
    public DataSourceType DataSourceType { get; set; } = DataSourceType.YouTube;
    public int UploadAttempts { get; set; } = 0;
    public DateTime? LastUploadAttempt { get; set; }
    public decimal DataEstimatedValue { get; set; } = 0;
    public decimal DeduplicationScore { get; set; } = 0;
    public string? DataIntegrityHash { get; set; }
    public IntegrityStatus IntegrityStatus { get; set; } = IntegrityStatus.Pending;
    public string? IntegrityReason { get; set; }
    public bool HasDonated { get; set; }
}
