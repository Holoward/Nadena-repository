using Application.Features.Reviews.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Reviews.Queries.GetDatasetReviews;

public class GetDatasetReviewsQuery : IRequest<ServiceResponse<List<ReviewDto>>>
{
    public int DatasetId { get; set; }
}

public class GetDatasetReviewsQueryHandler : IRequestHandler<GetDatasetReviewsQuery, ServiceResponse<List<ReviewDto>>>
{
    private readonly IReviewRepository _reviewRepository;

    public GetDatasetReviewsQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<ServiceResponse<List<ReviewDto>>> Handle(GetDatasetReviewsQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _reviewRepository.GetByDatasetIdAsync(request.DatasetId);

        var dtos = reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            DatasetId = r.DatasetId,
            BuyerId = r.BuyerId,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        }).ToList();

        return new ServiceResponse<List<ReviewDto>>(dtos);
    }
}
