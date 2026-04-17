using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Licensing.Commands.RevokeLicense;

public class RevokeLicenseCommand : IRequest<ServiceResponse<bool>>
{
    public Guid LicenseId { get; set; }
}

public class RevokeLicenseCommandHandler : IRequestHandler<RevokeLicenseCommand, ServiceResponse<bool>>
{
    private readonly IDataLicenseRepository _licenseRepository;
    private readonly IRepositoryAsync<Domain.Entities.ApiKey> _apiKeyRepository;

    public RevokeLicenseCommandHandler(
        IDataLicenseRepository licenseRepository,
        IRepositoryAsync<Domain.Entities.ApiKey> apiKeyRepository)
    {
        _licenseRepository = licenseRepository;
        _apiKeyRepository = apiKeyRepository;
    }

    public async Task<ServiceResponse<bool>> Handle(RevokeLicenseCommand request, CancellationToken cancellationToken)
    {
        var license = await _licenseRepository.GetByIdAsync(request.LicenseId);
        if (license == null)
            return new ServiceResponse<bool>("License not found.");

        // Revoke the API key so access immediately stops
        var apiKeys = await _apiKeyRepository.ListAsync();
        var apiKey = apiKeys.FirstOrDefault(k => k.Id == license.ApiKeyId);
        if (apiKey != null)
        {
            apiKey.IsRevoked = true;
            await _apiKeyRepository.UpdateAsync(apiKey);
        }

        license.Status = Domain.Enums.LicenseStatus.Revoked;
        await _licenseRepository.UpdateAsync(license);

        return new ServiceResponse<bool>(true);
    }
}
