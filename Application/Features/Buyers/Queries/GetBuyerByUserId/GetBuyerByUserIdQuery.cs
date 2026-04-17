using Application.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Application.Exceptions;
using Domain.Entities;
using MediatR;
using Ardalis.Specification;

namespace Application.Features.Buyers.Queries.GetBuyerByUserId;

public class GetBuyerByUserIdQuery : IRequest<ServiceResponse<BuyerDto>>
{
    public Guid UserId { get; set; }
}

public class GetBuyerByUserIdQueryHandler : IRequestHandler<GetBuyerByUserIdQuery, ServiceResponse<BuyerDto>>
{
    private readonly IBuyerRepository _buyerRepository;

    public GetBuyerByUserIdQueryHandler(IBuyerRepository buyerRepository)
    {
        _buyerRepository = buyerRepository;
    }

    public async Task<ServiceResponse<BuyerDto>> Handle(GetBuyerByUserIdQuery request, CancellationToken cancellationToken)
    {
        var buyer = await _buyerRepository.GetByUserIdAsync(request.UserId.ToString());
        
        if (buyer == null) throw new ApiException($"Data Client profile not found with UserId {request.UserId}");

        var buyerDto = new BuyerDto
        {
            Id = buyer.Id,
            UserId = buyer.UserId,
            CompanyName = buyer.CompanyName,
            UseCase = buyer.UseCase,
            Website = buyer.Website
        };

        return new ServiceResponse<BuyerDto>(buyerDto);
    }
}

public class BuyerByUserIdSpec : Specification<Buyer>
{
    public BuyerByUserIdSpec(string userId)
    {
        Query.Where(b => b.UserId == userId);
    }
}
