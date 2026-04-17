using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;

namespace Application.Features.Datasets.Queries.GetAllDatasets;

public class GetAllDatasetsQuery : IRequest<ServiceResponse<PaginatedResult<DatasetDto>>>
{
    public PaginationParams PaginationParams { get; set; }
    public string? Language { get; set; }
    public string? ContentCategory { get; set; }
    public int? MinCommentCount { get; set; }
    public decimal? MaxPrice { get; set; }
}

public class GetAllDatasetsQueryHandler : IRequestHandler<GetAllDatasetsQuery, ServiceResponse<PaginatedResult<DatasetDto>>>
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IReviewRepository _reviewRepository;

    public GetAllDatasetsQueryHandler(IDatasetRepository datasetRepository, IReviewRepository reviewRepository)
    {
        _datasetRepository = datasetRepository;
        _reviewRepository = reviewRepository;
    }

    public async Task<ServiceResponse<PaginatedResult<DatasetDto>>> Handle(GetAllDatasetsQuery request, CancellationToken cancellationToken)
    {
        var datasets = await _datasetRepository.GetAllAsync();

        // Apply filters
        var filtered = datasets.AsEnumerable();

        if (!string.IsNullOrEmpty(request.Language))
        {
            filtered = filtered.Where(d => d.Language != null && d.Language.Equals(request.Language, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(request.ContentCategory))
        {
            filtered = filtered.Where(d => d.IntendedUseCases != null && d.IntendedUseCases.Contains(request.ContentCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (request.MinCommentCount.HasValue)
        {
            filtered = filtered.Where(d => d.CommentCount >= request.MinCommentCount.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            filtered = filtered.Where(d => d.Price <= request.MaxPrice.Value);
        }

        var filteredList = filtered.ToList();
        var datasetDtos = new List<DatasetDto>();

        foreach (var d in filteredList)
        {
            var avgRating = await _reviewRepository.GetAverageRatingAsync(d.Id);
            var reviewCount = await _reviewRepository.GetReviewCountAsync(d.Id);

            datasetDtos.Add(new DatasetDto
            {
                Id = d.Id,
                Title = d.Title,
                Description = d.Description,
                VolunteerCount = d.VolunteerCount,
                CommentCount = d.CommentCount,
                Price = d.Price,
                Status = d.Status,
                BuyerReference = d.BuyerReference,
                CreatedAt = d.Created,
                Language = d.Language ?? "English",
                GeographicCoverage = d.GeographicCoverage ?? "Global",
                DateRangeStart = d.DateRangeStart,
                DateRangeEnd = d.DateRangeEnd,
                UpdateFrequency = d.UpdateFrequency ?? "OneTime",
                IntendedUseCases = d.IntendedUseCases,
                DataFormat = d.DataFormat ?? "CSV",
                SchemaDescription = d.SchemaDescription,
                AverageRating = avgRating,
                ReviewCount = reviewCount,
                ProvenanceDownloadUrl = $"/datasets/provenance_{d.Id}.json"
            });
        }

        var paginatedResult = new PaginatedResult<DatasetDto>
        {
            Data = datasetDtos.Skip((request.PaginationParams.Page - 1) * request.PaginationParams.PageSize)
                .Take(request.PaginationParams.PageSize).ToList(),
            TotalCount = datasetDtos.Count,
            Page = request.PaginationParams.Page,
            PageSize = request.PaginationParams.PageSize
        };

        return new ServiceResponse<PaginatedResult<DatasetDto>>(paginatedResult);
    }
}
