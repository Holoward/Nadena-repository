using Application.DTOs;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Volunteers.Queries.ExportMyData;

public class ExportMyDataQuery : IRequest<ServiceResponse<VolunteerDataExportDto>>
{
    public int VolunteerId { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
}
