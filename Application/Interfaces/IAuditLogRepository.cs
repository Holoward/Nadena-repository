namespace Application.Interfaces;

/// <summary>
/// Repository interface for AuditLog queries
/// </summary>
public interface IAuditLogRepository
{
    Task<(List<AuditLogListItem> Items, int TotalCount)> GetAuditLogsAsync(
        string? userId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for audit log list items
/// </summary>
public class AuditLogListItem
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string Action { get; set; }
    public string EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
