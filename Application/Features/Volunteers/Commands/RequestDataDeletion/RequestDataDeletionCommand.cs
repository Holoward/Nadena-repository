using Application.Wrappers;
using MediatR;

namespace Application.Features.Volunteers.Commands.RequestDataDeletion;

public class RequestDataDeletionCommand : IRequest<ServiceResponse<bool>>
{
    public int VolunteerId { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
}
