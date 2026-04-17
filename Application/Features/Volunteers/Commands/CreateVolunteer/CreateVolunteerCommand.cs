using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Volunteers.Commands.CreateVolunteer;

public class CreateVolunteerCommand : IRequest<ServiceResponse<VolunteerDto>>
{
    public string UserId { get; set; }
    public string YouTubeAccountAge { get; set; }
    public int CommentCountEstimate { get; set; }
    public string ContentTypes { get; set; }
}

public class CreateVolunteerCommandHandler : IRequestHandler<CreateVolunteerCommand, ServiceResponse<VolunteerDto>>
{
    private readonly IVolunteerRepository _volunteerRepository;

    public CreateVolunteerCommandHandler(IVolunteerRepository volunteerRepository)
    {
        _volunteerRepository = volunteerRepository;
    }

    public async Task<ServiceResponse<VolunteerDto>> Handle(CreateVolunteerCommand request, CancellationToken cancellationToken)
    {
        var volunteer = new Volunteer
        {
            UserId = request.UserId,
            YouTubeAccountAge = InputSanitizer.SanitizeString(request.YouTubeAccountAge),
            CommentCountEstimate = request.CommentCountEstimate.ToString(),
            ContentTypes = InputSanitizer.SanitizeString(request.ContentTypes),
            Status = VolunteerStatus.Registered,
            PaymentSent = false,
            BuyerReference = string.Empty,
            Notes = string.Empty,
            FileLink = string.Empty
        };

        await _volunteerRepository.AddAsync(volunteer);

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
