using Application.Wrappers;
using MediatR;

namespace Application.Features.Donation.Commands.CreateDonation;

public class CreateDonationCommand : IRequest<ServiceResponse<Guid>>
{
    public Guid ContributorId { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
}