using Application.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Application.Exceptions;
using Domain.Enums;
using MediatR;

namespace Application.Features.Volunteers.Commands.UpdateVolunteerStatus;

public class UpdateVolunteerStatusCommand : IRequest<ServiceResponse<VolunteerDto>>
{
    public int Id { get; set; }
    public string Status { get; set; }
    public string? FileLink { get; set; }
    public string? BuyerReference { get; set; }
    public bool PaymentSent { get; set; }
    public string? Notes { get; set; }
}

public class UpdateVolunteerStatusCommandHandler : IRequestHandler<UpdateVolunteerStatusCommand, ServiceResponse<VolunteerDto>>
{
    private readonly IVolunteerRepository _volunteerRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;

    public UpdateVolunteerStatusCommandHandler(
        IVolunteerRepository volunteerRepository,
        IAuditLogService auditLogService,
        IEmailService emailService)
    {
        _volunteerRepository = volunteerRepository;
        _auditLogService = auditLogService;
        _emailService = emailService;
    }

    public async Task<ServiceResponse<VolunteerDto>> Handle(UpdateVolunteerStatusCommand request, CancellationToken cancellationToken)
    {
        var volunteer = await _volunteerRepository.GetByIdAsync(request.Id);
        if (volunteer == null) throw new ApiException($"Data Contributor not found with Id {request.Id}");

        var oldStatus = volunteer.Status.ToString();
        
        bool statusChangedToActivated = false;
        if (Enum.TryParse<VolunteerStatus>(request.Status, true, out var newStatus))
        {
            if (volunteer.Status != VolunteerStatus.Activated && newStatus == VolunteerStatus.Activated)
            {
                volunteer.ActivatedDate = DateTime.Now;
                statusChangedToActivated = true;
            }
            volunteer.Status = newStatus;
        }

        var newStatusStr = volunteer.Status.ToString();

        volunteer.FileLink = request.FileLink;
        volunteer.BuyerReference = request.BuyerReference;
        volunteer.PaymentSent = request.PaymentSent;
        volunteer.Notes = request.Notes;

        await _volunteerRepository.UpdateAsync(volunteer);

        // Audit logging for status update
        await _auditLogService.LogAsync(
            action: "StatusUpdated",
            entityType: "Volunteer",
            entityId: volunteer.Id.ToString(),
            success: true,
            userId: volunteer.UserId,
            oldValues: "{\"Status\":\"" + oldStatus + "\"}",
            newValues: "{\"Status\":\"" + newStatusStr + "\"}");

        // Send activation email if status changed to Activated
        if (statusChangedToActivated)
        {
            var userInfo = await _volunteerRepository.GetUserInfoByVolunteerIdAsync(volunteer.Id);
            if (userInfo.HasValue)
            {
                var googleTakeoutInstructions = "1. Go to https://takeout.google.com\n2. Sign in with your Google account\n3. Select the data you want to export\n4. Choose ZIP as the delivery method\n5. Download and upload the file to Nadena";
                await _emailService.SendActivationEmailAsync(userInfo.Value.Email, userInfo.Value.FullName, googleTakeoutInstructions);
            }
        }

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
