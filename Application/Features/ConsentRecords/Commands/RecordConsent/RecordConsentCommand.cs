using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;

namespace Application.Features.ConsentRecords.Commands.RecordConsent;

public class RecordConsentCommand : IRequest<ServiceResponse<bool>>
{
    public Guid UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string ConsentText { get; set; }
    public string DocumentType { get; set; } = string.Empty; // TermsOfService | DataConsent
    public string FormVersion { get; set; } = "v1.0";
}

public class RecordConsentCommandHandler : IRequestHandler<RecordConsentCommand, ServiceResponse<bool>>
{
    private readonly IConsentRecordRepository _consentRepository;

    public RecordConsentCommandHandler(IConsentRecordRepository consentRepository)
    {
        _consentRepository = consentRepository;
    }

    public async Task<ServiceResponse<bool>> Handle(RecordConsentCommand request, CancellationToken cancellationToken)
    {
        var consent = new ConsentRecord
        {
            UserId = request.UserId.ToString(),
            IpAddress = request.IpAddress,
            ConsentText = request.ConsentText,
            DocumentType = request.DocumentType,
            FormVersion = request.FormVersion,
            AgreedAt = DateTime.UtcNow
        };

        await _consentRepository.AddAsync(consent);

        return new ServiceResponse<bool>(true);
    }
}
