namespace Application.Interfaces;

/// <summary>
/// Service for logging auditable actions in the system
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Logs an auditable action
    /// </summary>
    Task LogAsync(string action, string entityType, string entityId, bool success, 
        string userId = null, string oldValues = null, string newValues = null, 
        string errorMessage = null);
}
