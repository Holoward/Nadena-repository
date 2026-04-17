using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Persistence.Context;

namespace Persistence.Services;

/// <summary>
/// Service for logging auditable actions in the system
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditLogService> _logger;
    private readonly IDateTimeService _dateTimeService;

    public AuditLogService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditLogService> logger,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _dateTimeService = dateTimeService;
    }

    public async Task LogAsync(string action, string entityType, string entityId, bool success, 
        string userId = null, string oldValues = null, string newValues = null, 
        string errorMessage = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            string ipAddress = null;
            string userAgent = null;

            if (httpContext != null)
            {
                ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                userAgent = httpContext.Request.Headers.UserAgent.ToString();
            }

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = _dateTimeService.NowUtc,
                Success = success,
                ErrorMessage = errorMessage
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Never throw - log the error and continue
            _logger.LogError(ex, "Failed to write audit log for action {Action}. Error: {Message}", 
                action, ex.Message);
        }
    }
}
