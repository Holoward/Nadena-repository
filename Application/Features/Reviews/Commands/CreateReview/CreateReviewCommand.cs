using Application.Features.Reviews.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;

namespace Application.Features.Reviews.Commands.CreateReview;

public class CreateReviewCommand : IRequest<ServiceResponse<ReviewDto>>
{
    public int DatasetId { get; set; }
    public Guid BuyerId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, ServiceResponse<ReviewDto>>
{
    private readonly IReviewRepository _reviewRepository;

    public CreateReviewCommandHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<ServiceResponse<ReviewDto>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = new Review
        {
            DatasetId = request.DatasetId,
            BuyerId = request.BuyerId,
            Rating = request.Rating,
            Comment = request.Comment,
            CreatedAt = DateTime.UtcNow
        };

        await _reviewRepository.AddAsync(review);

        var dto = new ReviewDto
        {
            Id = review.Id,
            DatasetId = review.DatasetId,
            BuyerId = review.BuyerId,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };

        return new ServiceResponse<ReviewDto>(dto, "Review submitted successfully");
    }
}
