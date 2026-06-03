using Application.Interfaces;
using Application.Wrappers;
using Domain.Enums;
using MediatR;

namespace Application.Features.Licensing.Queries.GetMyLicenses;

public class GetMyLicensesQuery : IRequest<ServiceResponse<IEnumerable<LicenseDto>>>
{
    public Guid BuyerUserId { get; set; }
}

public class LicenseDto
{
    public Guid Id { get; set; }
    public int DataPoolId { get; set; }
    public string DataPoolName { get; set; } = string.Empty;
    public DateTime LicensedFrom { get; set; }
    public DateTime LicensedUntil { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal VolunteerShare { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
}

public class GetMyLicensesQueryHandler : IRequestHandler<GetMyLicensesQuery, ServiceResponse<IEnumerable<LicenseDto>>>
{
    private readonly IDataLicenseRepository _licenseRepository;
    private readonly IDataPoolRepository _poolRepository;
    private readonly IBuyerRepository _buyerRepository;

    public GetMyLicensesQueryHandler(
        IDataLicenseRepository licenseRepository,
        IDataPoolRepository poolRepository,
        IBuyerRepository buyerRepository)
    {
        _licenseRepository = licenseRepository;
        _poolRepository = poolRepository;
        _buyerRepository = buyerRepository;
    }

    public async Task<ServiceResponse<IEnumerable<LicenseDto>>> Handle(GetMyLicensesQuery request, CancellationToken cancellationToken)
    {
        var buyer = await _buyerRepository.GetByUserIdAsync(request.BuyerUserId.ToString());
        if (buyer == null)
            return new ServiceResponse<IEnumerable<LicenseDto>>("Data client profile not found.");

        // Use UserId (string) as the buyer identifier since DataLicense uses string BuyerId
        var licenses = await _licenseRepository.GetByBuyerIdStringAsync(buyer.UserId);
        var pools = (await _poolRepository.GetAllAsync()).ToDictionary(p => p.Id, p => p.Name);

        var now = DateTime.UtcNow;
        var dtos = licenses.Select(l => new LicenseDto
        {
            Id = l.Id,
            DataPoolId = l.DataPoolId,
            DataPoolName = pools.TryGetValue(l.DataPoolId, out var name) ? name : "Unknown",
            LicensedFrom = l.LicensedFrom,
            LicensedUntil = l.LicensedUntil,
            AmountPaid = l.AmountPaid,
            VolunteerShare = l.VolunteerShare,
            Status = l.Status.ToString(),
            IsExpired = l.LicensedUntil < now
        });

        return new ServiceResponse<IEnumerable<LicenseDto>>(dtos);
    }
}
