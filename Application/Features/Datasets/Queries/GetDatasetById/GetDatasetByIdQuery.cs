using Application.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Application.Exceptions;
using Domain.Entities;
using MediatR;

namespace Application.Features.Datasets.Queries.GetDatasetById;

public class GetDatasetByIdQuery : IRequest<ServiceResponse<DatasetDto>>
{
    public int Id { get; set; }
}

public class GetDatasetByIdQueryHandler : IRequestHandler<GetDatasetByIdQuery, ServiceResponse<DatasetDto>>
{
    private readonly IDatasetRepository _datasetRepository;

    public GetDatasetByIdQueryHandler(IDatasetRepository datasetRepository)
    {
        _datasetRepository = datasetRepository;
    }

    public async Task<ServiceResponse<DatasetDto>> Handle(GetDatasetByIdQuery request, CancellationToken cancellationToken)
    {
        var dataset = await _datasetRepository.GetByIdAsync(request.Id);
        if (dataset == null) throw new ApiException($"Dataset not found with Id {request.Id}");

        var datasetDto = new DatasetDto
        {
            Id = dataset.Id,
            Title = dataset.Title,
            Description = dataset.Description,
            VolunteerCount = dataset.VolunteerCount,
            CommentCount = dataset.CommentCount,
            Price = dataset.Price,
            Status = dataset.Status,
            BuyerReference = dataset.BuyerReference,
            CreatedAt = dataset.Created,
            Language = dataset.Language ?? "English",
            GeographicCoverage = dataset.GeographicCoverage ?? "Global",
            DateRangeStart = dataset.DateRangeStart,
            DateRangeEnd = dataset.DateRangeEnd,
            UpdateFrequency = dataset.UpdateFrequency ?? "OneTime",
            IntendedUseCases = dataset.IntendedUseCases,
            DataFormat = dataset.DataFormat ?? "CSV",
            SchemaDescription = dataset.SchemaDescription,
            ProvenanceDownloadUrl = $"/datasets/provenance_{dataset.Id}.json"
        };

        return new ServiceResponse<DatasetDto>(datasetDto);
    }
}
