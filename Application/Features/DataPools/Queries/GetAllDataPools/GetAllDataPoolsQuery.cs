using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.DataPools.Queries.GetAllDataPools;

public class GetAllDataPoolsQuery : IRequest<ServiceResponse<IEnumerable<DataPoolDto>>>
{
    public bool ActiveOnly { get; set; } = true;
}

public class DataPoolDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public decimal RevenueSharePercent { get; set; }
    public long ApproximateRecordCount { get; set; }
    public bool IsActive { get; set; }
}

public class GetAllDataPoolsQueryHandler : IRequestHandler<GetAllDataPoolsQuery, ServiceResponse<IEnumerable<DataPoolDto>>>
{
    private readonly IDataPoolRepository _poolRepository;

    public GetAllDataPoolsQueryHandler(IDataPoolRepository poolRepository)
    {
        _poolRepository = poolRepository;
    }

    public async Task<ServiceResponse<IEnumerable<DataPoolDto>>> Handle(GetAllDataPoolsQuery request, CancellationToken cancellationToken)
    {
        var pools = request.ActiveOnly
            ? await _poolRepository.GetAllActiveAsync()
            : await _poolRepository.GetAllAsync();

        var dtos = pools.Select(p => new DataPoolDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Category = p.Category,
            PricePerMonth = p.PricePerMonth,
            RevenueSharePercent = p.RevenueSharePercent,
            ApproximateRecordCount = p.ApproximateRecordCount,
            IsActive = p.IsActive
        });

        return new ServiceResponse<IEnumerable<DataPoolDto>>(dtos);
    }
}
