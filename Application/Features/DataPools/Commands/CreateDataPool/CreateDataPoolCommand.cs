using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;

namespace Application.Features.DataPools.Commands.CreateDataPool;

public class CreateDataPoolCommand : IRequest<ServiceResponse<CreateDataPoolResult>>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public decimal RevenueSharePercent { get; set; } = 75m;
}

public class CreateDataPoolResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public decimal RevenueSharePercent { get; set; }
}

public class CreateDataPoolCommandHandler : IRequestHandler<CreateDataPoolCommand, ServiceResponse<CreateDataPoolResult>>
{
    private readonly IDataPoolRepository _poolRepository;

    public CreateDataPoolCommandHandler(IDataPoolRepository poolRepository)
    {
        _poolRepository = poolRepository;
    }

    public async Task<ServiceResponse<CreateDataPoolResult>> Handle(CreateDataPoolCommand request, CancellationToken cancellationToken)
    {
        var pool = new DataPool
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            PricePerMonth = request.PricePerMonth,
            RevenueSharePercent = Math.Clamp(request.RevenueSharePercent, 50m, 90m),
            IsActive = true
        };

        await _poolRepository.AddAsync(pool);

        return new ServiceResponse<CreateDataPoolResult>(new CreateDataPoolResult
        {
            Id = pool.Id,
            Name = pool.Name,
            Category = pool.Category,
            PricePerMonth = pool.PricePerMonth,
            RevenueSharePercent = pool.RevenueSharePercent
        });
    }
}
