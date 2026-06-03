using Application.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Application.Exceptions;
using Domain.Entities;
using MediatR;

namespace Application.Features.Datasets.Commands.UpdateDataset;

public class UpdateDatasetCommand : IRequest<ServiceResponse<DatasetDto>>
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int VolunteerCount { get; set; }
    public int CommentCount { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; }
    public string BuyerReference { get; set; }
}

public class UpdateDatasetCommandHandler : IRequestHandler<UpdateDatasetCommand, ServiceResponse<DatasetDto>>
{
    private readonly IDatasetRepository _datasetRepository;

    public UpdateDatasetCommandHandler(IDatasetRepository datasetRepository)
    {
        _datasetRepository = datasetRepository;
    }

    public async Task<ServiceResponse<DatasetDto>> Handle(UpdateDatasetCommand request, CancellationToken cancellationToken)
    {
        var dataset = await _datasetRepository.GetByIdAsync(request.Id);
        if (dataset == null) throw new ApiException($"Dataset not found with Id {request.Id}");

        dataset.Title = request.Title;
        dataset.Description = request.Description;
        dataset.VolunteerCount = request.VolunteerCount;
        dataset.CommentCount = request.CommentCount;
        dataset.Price = request.Price;
        dataset.Status = request.Status;
        dataset.BuyerReference = request.BuyerReference;

        await _datasetRepository.UpdateAsync(dataset);

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
            CreatedAt = dataset.Created
        };

        return new ServiceResponse<DatasetDto>(datasetDto);
    }
}
