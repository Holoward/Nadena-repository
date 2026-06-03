using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Admin.Queries.GetAuditLogs;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, ServiceResponse<GetAuditLogsResult>>
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ServiceResponse<GetAuditLogsResult>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _auditLogRepository.GetAuditLogsAsync(
            userId: request.UserId,
            action: request.Action,
            from: request.From,
            to: request.To,
            page: request.Page,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        var auditLogDtos = items.Select(a => new AuditLogDto
        {
            Id = a.Id,
            UserId = a.UserId,
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            OldValues = a.OldValues,
            NewValues = a.NewValues,
            IpAddress = a.IpAddress,
            UserAgent = a.UserAgent,
            Timestamp = a.Timestamp,
            Success = a.Success,
            ErrorMessage = a.ErrorMessage
        }).ToList();

        var result = new GetAuditLogsResult
        {
            AuditLogs = auditLogDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return new ServiceResponse<GetAuditLogsResult>(result);
    }
}
