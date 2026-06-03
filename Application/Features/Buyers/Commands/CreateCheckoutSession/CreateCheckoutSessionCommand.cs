using Application.Wrappers;
using MediatR;

namespace Application.Features.Buyers.Commands.CreateCheckoutSession;

public class CreateCheckoutSessionCommand : IRequest<ServiceResponse<string>>
{
    public Guid DatasetId { get; set; }
    public string BuyerUserId { get; set; }
}
