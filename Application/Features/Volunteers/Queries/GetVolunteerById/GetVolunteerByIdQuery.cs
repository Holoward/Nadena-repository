using Application.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Application.Exceptions;
using MediatR;

namespace Application.Features.Volunteers.Queries.GetVolunteerById;

public class GetVolunteerByIdQuery : IRequest<ServiceResponse<VolunteerDto>>
{
    public int Id { get; set; }
}

public class GetVolunteerByIdQueryHandler : IRequestHandler<GetVolunteerByIdQuery, ServiceResponse<VolunteerDto>>
{
    private readonly IVolunteerRepository _volunteerRepository;

    public GetVolunteerByIdQueryHandler(IVolunteerRepository volunteerRepository)
    {
        _volunteerRepository = volunteerRepository;
    }

    public async Task<ServiceResponse<VolunteerDto>> Handle(GetVolunteerByIdQuery request, CancellationToken cancellationToken)
    {
        var volunteer = await _volunteerRepository.GetByIdAsync(request.Id);
        if (volunteer == null) throw new ApiException($"Data Contributor not found with Id {request.Id}");

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
