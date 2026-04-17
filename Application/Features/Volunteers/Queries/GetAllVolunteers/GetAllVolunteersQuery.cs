using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Volunteers.Queries.GetAllVolunteers;

public class GetAllVolunteersQuery : IRequest<ServiceResponse<PaginatedResult<VolunteerDto>>>
{
    public PaginationParams PaginationParams { get; set; }
}

public class GetAllVolunteersQueryHandler : IRequestHandler<GetAllVolunteersQuery, ServiceResponse<PaginatedResult<VolunteerDto>>>
{
    private readonly IVolunteerRepository _volunteerRepository;

    public GetAllVolunteersQueryHandler(IVolunteerRepository volunteerRepository)
    {
        _volunteerRepository = volunteerRepository;
    }

    public async Task<ServiceResponse<PaginatedResult<VolunteerDto>>> Handle(GetAllVolunteersQuery request, CancellationToken cancellationToken)
    {
        var volunteers = await _volunteerRepository.GetAllAsync();
        var volunteerDtos = volunteers.Select(v => new VolunteerDto
        {
            Id = v.Id,
            UserId = v.UserId,
            Status = v.Status.ToString(),
            YouTubeAccountAge = v.YouTubeAccountAge,
            CommentCountEstimate = v.CommentCountEstimate,
            ContentTypes = v.ContentTypes,
            FileLink = v.FileLink,
            ActivatedDate = v.ActivatedDate,
            BuyerReference = v.BuyerReference,
            PaymentSent = v.PaymentSent,
            Notes = v.Notes
        }).ToList();

        var paginatedResult = new PaginatedResult<VolunteerDto>
        {
            Data = volunteerDtos.Skip((request.PaginationParams.Page - 1) * request.PaginationParams.PageSize)
                .Take(request.PaginationParams.PageSize).ToList(),
            TotalCount = volunteerDtos.Count,
            Page = request.PaginationParams.Page,
            PageSize = request.PaginationParams.PageSize
        };

        return new ServiceResponse<PaginatedResult<VolunteerDto>>(paginatedResult);
    }
}
