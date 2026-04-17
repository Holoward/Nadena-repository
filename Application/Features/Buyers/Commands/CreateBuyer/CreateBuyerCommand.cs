using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;

namespace Application.Features.Buyers.Commands.CreateBuyer;

public class CreateBuyerCommand : IRequest<ServiceResponse<BuyerDto>>
{
    public Guid UserId { get; set; }
    public string CompanyName { get; set; }
    public string UseCase { get; set; }
    public string Website { get; set; }
}

public class CreateBuyerCommandHandler : IRequestHandler<CreateBuyerCommand, ServiceResponse<BuyerDto>>
{
    private readonly IBuyerRepository _buyerRepository;

    public CreateBuyerCommandHandler(IBuyerRepository buyerRepository)
    {
        _buyerRepository = buyerRepository;
    }

    public async Task<ServiceResponse<BuyerDto>> Handle(CreateBuyerCommand request, CancellationToken cancellationToken)
    {
        var buyer = new Buyer
        {
            UserId = request.UserId.ToString(),
            CompanyName = InputSanitizer.SanitizeString(request.CompanyName),
            UseCase = InputSanitizer.SanitizeString(request.UseCase),
            Website = InputSanitizer.SanitizeString(request.Website)
        };

        await _buyerRepository.AddAsync(buyer);

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
