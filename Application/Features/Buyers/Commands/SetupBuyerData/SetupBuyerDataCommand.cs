using Application.Exceptions;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Buyers.Commands.SetupBuyerData;

public class SetupBuyerDataCommand : IRequest<ServiceResponse<bool>>
{
    public string UserId { get; set; } = string.Empty;
    public List<string> DataInterests { get; set; } = new();
}

public class SetupBuyerDataCommandHandler : IRequestHandler<SetupBuyerDataCommand, ServiceResponse<bool>>
{
    private readonly IBuyerRepository _buyerRepository;

    public SetupBuyerDataCommandHandler(IBuyerRepository buyerRepository)
    {
        _buyerRepository = buyerRepository;
    }

    public async Task<ServiceResponse<bool>> Handle(SetupBuyerDataCommand request, CancellationToken cancellationToken)
    {
        var buyer = await _buyerRepository.GetByUserIdAsync(request.UserId);
        if (buyer == null) throw new ApiException($"Data Client profile not found with UserId {request.UserId}");

        buyer.UseCase = string.Join(", ", request.DataInterests);

        await _buyerRepository.UpdateAsync(buyer);

        return new ServiceResponse<bool>(true, "Data client setup completed successfully.");
    }
}
