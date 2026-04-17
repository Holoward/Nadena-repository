using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Volunteers.Commands.RequestDataDeletion;

public class RequestDataDeletionCommandHandler : IRequestHandler<RequestDataDeletionCommand, ServiceResponse<bool>>
{
    private readonly IGdprService _gdprService;

    public RequestDataDeletionCommandHandler(IGdprService gdprService)
    {
        _gdprService = gdprService;
    }

    public async Task<ServiceResponse<bool>> Handle(RequestDataDeletionCommand request, CancellationToken cancellationToken)
    {
        var result = await _gdprService.DeleteVolunteerDataAsync(
            request.VolunteerId, 
            request.RequestedByUserId, 
            cancellationToken);

        return new ServiceResponse<bool>(result, "Data deletion request processed successfully. Your data has been anonymized.");
    }
}
