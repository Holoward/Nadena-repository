using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// A buyer review of a purchased dataset
/// </summary>
public class Review : AuditableBaseEntity
{
    public int DatasetId { get; set; }
    public Guid BuyerId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
