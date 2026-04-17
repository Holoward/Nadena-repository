using Application.Features.Datasets.DTOs;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Datasets.Commands.AnalyzeDataset;

public class AnalyzeDatasetCommand : IRequest<ServiceResponse<DatasetAnalysisDto>>
{
    public int DatasetId { get; set; }
}
