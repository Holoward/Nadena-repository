using Application.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Volunteers.Queries.ExportMyData;

public class ExportMyDataQueryHandler : IRequestHandler<ExportMyDataQuery, ServiceResponse<VolunteerDataExportDto>>
{
    private readonly IGdprService _gdprService;

    public ExportMyDataQueryHandler(IGdprService gdprService)
    {
        _gdprService = gdprService;
    }

    public async Task<ServiceResponse<VolunteerDataExportDto>> Handle(ExportMyDataQuery request, CancellationToken cancellationToken)
    {
        var exportData = await _gdprService.ExportVolunteerDataAsync(
            request.VolunteerId, 
            request.RequestedByUserId, 
            cancellationToken);

        return new ServiceResponse<VolunteerDataExportDto>(exportData);
    }
}
