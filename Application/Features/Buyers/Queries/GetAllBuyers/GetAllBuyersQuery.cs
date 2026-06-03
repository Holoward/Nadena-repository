using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;

namespace Application.Features.Buyers.Queries.GetAllBuyers;

public class GetAllBuyersQuery : IRequest<ServiceResponse<PaginatedResult<BuyerDto>>>
{
    public PaginationParams PaginationParams { get; set; }
}

public class GetAllBuyersQueryHandler : IRequestHandler<GetAllBuyersQuery, ServiceResponse<PaginatedResult<BuyerDto>>>
{
    private readonly IBuyerRepository _buyerRepository;

    public GetAllBuyersQueryHandler(IBuyerRepository buyerRepository)
    {
        _buyerRepository = buyerRepository;
    }

    public async Task<ServiceResponse<PaginatedResult<BuyerDto>>> Handle(GetAllBuyersQuery request, CancellationToken cancellationToken)
    {
        var buyers = await _buyerRepository.GetAllAsync();
        var buyerDtos = buyers.Select(b => new BuyerDto
        {
            Id = b.Id,
            UserId = b.UserId,
            CompanyName = b.CompanyName,
            UseCase = b.UseCase,
            Website = b.Website
        }).ToList();

        var paginatedResult = new PaginatedResult<BuyerDto>
        {
            Data = buyerDtos.Skip((request.PaginationParams.Page - 1) * request.PaginationParams.PageSize)
                .Take(request.PaginationParams.PageSize).ToList(),
            TotalCount = buyerDtos.Count,
            Page = request.PaginationParams.Page,
            PageSize = request.PaginationParams.PageSize
        };

        return new ServiceResponse<PaginatedResult<BuyerDto>>(paginatedResult);
    }
}
