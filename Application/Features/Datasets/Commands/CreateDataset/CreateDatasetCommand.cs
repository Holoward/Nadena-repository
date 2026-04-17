using Application.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;

namespace Application.Features.Datasets.Commands.CreateDataset;

public class CreateDatasetCommand : IRequest<ServiceResponse<DatasetDto>>
{
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
}

public class CreateDatasetCommandHandler : IRequestHandler<CreateDatasetCommand, ServiceResponse<DatasetDto>>
{
    private readonly IDatasetRepository _datasetRepository;

    public CreateDatasetCommandHandler(IDatasetRepository datasetRepository)
    {
        _datasetRepository = datasetRepository;
    }

    public async Task<ServiceResponse<DatasetDto>> Handle(CreateDatasetCommand request, CancellationToken cancellationToken)
    {
        var dataset = new Dataset
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            Status = "Available",
            BuyerReference = string.Empty,
            VolunteerCount = 0,
            CommentCount = 0
        };

        await _datasetRepository.AddAsync(dataset);

        var datasetDto = new DatasetDto
        {
            Id = dataset.Id,
            Title = dataset.Title,
            Description = dataset.Description,
            Price = dataset.Price,
            Status = dataset.Status,
            BuyerReference = dataset.BuyerReference,
            VolunteerCount = dataset.VolunteerCount,
            CommentCount = dataset.CommentCount,
            CreatedAt = dataset.Created
        };

        return new ServiceResponse<DatasetDto>(datasetDto);
    }
}
