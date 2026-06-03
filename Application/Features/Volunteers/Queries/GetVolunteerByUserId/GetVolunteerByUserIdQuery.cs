using Application.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Application.Exceptions;
using MediatR;

namespace Application.Features.Volunteers.Queries.GetVolunteerByUserId;

public class GetVolunteerByUserIdQuery : IRequest<ServiceResponse<VolunteerDto>>
{
    public string UserId { get; set; }
}

public class GetVolunteerByUserIdQueryHandler : IRequestHandler<GetVolunteerByUserIdQuery, ServiceResponse<VolunteerDto>>
{
    private readonly IVolunteerRepository _volunteerRepository;

    public GetVolunteerByUserIdQueryHandler(IVolunteerRepository volunteerRepository)
    {
        _volunteerRepository = volunteerRepository;
    }

    public async Task<ServiceResponse<VolunteerDto>> Handle(GetVolunteerByUserIdQuery request, CancellationToken cancellationToken)
    {
        var volunteer = await _volunteerRepository.GetByUserIdAsync(request.UserId);
        if (volunteer == null) throw new ApiException($"Data Contributor profile not found with UserId {request.UserId}");

        var volunteerDto = new VolunteerDto
        {
            Id = volunteer.Id,
            UserId = volunteer.UserId,
            Status = volunteer.Status.ToString(),
            YouTubeAccountAge = volunteer.YouTubeAccountAge,
            CommentCountEstimate = volunteer.CommentCountEstimate,
            ContentTypes = volunteer.ContentTypes,
            FileLink = volunteer.FileLink,
            ActivatedDate = volunteer.ActivatedDate,
            BuyerReference = volunteer.BuyerReference,
            PaymentSent = volunteer.PaymentSent,
            Notes = volunteer.Notes
        };

        return new ServiceResponse<VolunteerDto>(volunteerDto);
    }
}
