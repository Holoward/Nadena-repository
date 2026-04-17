using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Licensing.Commands.PurchaseLicense;

public class PurchaseLicenseCommand : IRequest<ServiceResponse<PurchaseLicenseResult>>
{
    public int DataPoolId { get; set; }

    /// <summary>ASP.NET Identity UserId (string GUID from JWT)</summary>
    public string BuyerUserId { get; set; } = string.Empty;

    /// <summary>Number of months to license. Min 1, Max 24.</summary>
    public int Months { get; set; } = 1;
}

public class PurchaseLicenseResult
{
    public Guid LicenseId { get; set; }
    public string RawApiKey { get; set; } = string.Empty;  // Shown ONCE — buyer must save this
    public string DataPoolName { get; set; } = string.Empty;
    public DateTime LicensedFrom { get; set; }
    public DateTime LicensedUntil { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal VolunteerShare { get; set; }
    public string DistributionTxRef { get; set; } = string.Empty;
}

public class PurchaseLicenseCommandHandler : IRequestHandler<PurchaseLicenseCommand, ServiceResponse<PurchaseLicenseResult>>
{
    private readonly IDataPoolRepository _poolRepository;
    private readonly IDataLicenseRepository _licenseRepository;
    private readonly IApiKeyService _apiKeyService;
    private readonly IBlockchainService _blockchainService;
    private readonly IRepositoryAsync<ApiKey> _apiKeyRepository;
    private readonly IBuyerRepository _buyerRepository;

    public PurchaseLicenseCommandHandler(
        IDataPoolRepository poolRepository,
        IDataLicenseRepository licenseRepository,
        IApiKeyService apiKeyService,
        IBlockchainService blockchainService,
        IRepositoryAsync<ApiKey> apiKeyRepository,
        IBuyerRepository buyerRepository)
    {
        _poolRepository = poolRepository;
        _licenseRepository = licenseRepository;
        _apiKeyService = apiKeyService;
        _blockchainService = blockchainService;
        _apiKeyRepository = apiKeyRepository;
        _buyerRepository = buyerRepository;
    }

    public async Task<ServiceResponse<PurchaseLicenseResult>> Handle(PurchaseLicenseCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate pool exists and is active
        var pool = await _poolRepository.GetByIdAsync(request.DataPoolId);
        if (pool == null || !pool.IsActive)
            return new ServiceResponse<PurchaseLicenseResult>("Data pool not found or is not available.");

        // 2. Validate buyer exists (UserId is string in Identity)
        var buyer = await _buyerRepository.GetByUserIdAsync(request.BuyerUserId);
        if (buyer == null)
            return new ServiceResponse<PurchaseLicenseResult>("Data client profile not found. Please complete your registration first.");

        // 3. Calculate amounts
        var months = Math.Max(1, request.Months);
        var totalAmount = pool.PricePerMonth * months;
        var volunteerShare = Math.Round(totalAmount * (pool.RevenueSharePercent / 100m), 2);
        var platformFee = totalAmount - volunteerShare;

        var now = DateTime.UtcNow;
        var licensedUntil = now.AddMonths(months);

        // 4. Generate hashed API key
        var (rawKey, keyHash, keyPrefix) = _apiKeyService.GenerateApiKey();

        // ApiKey.BuyerId stores the Identity UserId string
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            BuyerId = buyer.UserId,
            KeyPrefix = keyPrefix,
            KeyHash = keyHash,
            ExpiresAt = licensedUntil,
            IsRevoked = false,
            Created = now,
            CreatedBy = buyer.UserId,
            LastModified = now,
            LastModifiedBY = buyer.UserId
        };
        await _apiKeyRepository.AddAsync(apiKey);

        // 5. Create the license record
        var license = new DataLicense
        {
            Id = Guid.NewGuid(),
            BuyerId = buyer.UserId,
            DataPoolId = request.DataPoolId,
            ApiKeyId = apiKey.Id,
            LicensedFrom = now,
            LicensedUntil = licensedUntil,
            AmountPaid = totalAmount,
            PlatformFee = platformFee,
            VolunteerShare = volunteerShare,
            MonthsLicensed = months,
            Status = LicenseStatus.Active,
            Created = now,
            CreatedBy = buyer.UserId,
            LastModified = now,
            LastModifiedBY = buyer.UserId
        };
        await _licenseRepository.AddAsync(license);

        // 6. Trigger automated revenue distribution to volunteers
        var txRef = await _blockchainService.DistributeRevenueAsync(
            license.Id,
            pool.Id,
            totalAmount,
            pool.RevenueSharePercent);

        license.DistributionTxRef = txRef;
        await _licenseRepository.UpdateAsync(license);

        return new ServiceResponse<PurchaseLicenseResult>(new PurchaseLicenseResult
        {
            LicenseId = license.Id,
            RawApiKey = rawKey,
            DataPoolName = pool.Name,
            LicensedFrom = license.LicensedFrom,
            LicensedUntil = license.LicensedUntil,
            AmountPaid = totalAmount,
            PlatformFee = platformFee,
            VolunteerShare = volunteerShare,
            DistributionTxRef = txRef
        });
    }
}
