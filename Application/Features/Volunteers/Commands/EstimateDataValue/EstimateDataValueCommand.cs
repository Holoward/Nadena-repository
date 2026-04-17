using Application.Features.Volunteers.DTOs;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Volunteers.Commands.EstimateDataValue;

public class EstimateDataValueCommand : IRequest<ServiceResponse<DataValueEstimateDto>>
{
    public int VolunteerId { get; set; }
    public int CommentCountEstimate { get; set; }
    public string ContentTypes { get; set; } = string.Empty;
    public string YouTubeAccountAge { get; set; } = string.Empty;
}
