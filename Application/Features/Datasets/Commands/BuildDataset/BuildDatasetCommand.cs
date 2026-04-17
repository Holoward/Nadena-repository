using Application.Wrappers;
using Domain.Enums;
using MediatR;

namespace Application.Features.Datasets.Commands.BuildDataset;

public class BuildDatasetCommand : IRequest<ServiceResponse<BuildDatasetResult>>
{
    public List<Guid> VolunteerIds { get; set; }
    public string DatasetName { get; set; }
    public decimal Price { get; set; }
    public DataSourceType DataSourceType { get; set; } = DataSourceType.YouTube;
}

public class BuildDatasetResult
{
    public int DatasetId { get; set; }
    public string DownloadUrl { get; set; }
}
