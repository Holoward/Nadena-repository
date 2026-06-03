using Application.Features.ConsentRecords.Commands.RecordConsent;
using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Authorize(Roles = "Data Contributor")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OnboardingController : ControllerBase
{
    private const string TermsDocumentType = "TermsOfService";
    private const string ConsentDocumentType = "DataConsent";
    private const string FormVersion = "v1.0";

    private const string TermsText =
        "Terms of Service — Coming Soon. This document will be updated before public launch.";

    private const string ConsentText =
        "Data Consent Form — Coming Soon. This document will detail exactly what data is collected, how it is anonymized, and how it is used.";

    private readonly IConsentRecordRepository _consentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _mediator;

    public OnboardingController(
        IConsentRecordRepository consentRepository,
        ICurrentUserService currentUserService,
        ISender mediator)
    {
        _consentRepository = consentRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    // GET: api/v1/Onboarding/status
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var termsAccepted = await _consentRepository.HasAcceptedAsync(userId, TermsDocumentType);
        var consentAccepted = await _consentRepository.HasAcceptedAsync(userId, ConsentDocumentType);

        string? nextStep = null;
        if (!termsAccepted)
        {
            nextStep = "/onboarding/terms";
        }
        else if (!consentAccepted)
        {
            nextStep = "/onboarding/consent";
        }

        return Ok(new
        {
            termsAccepted,
            consentAccepted,
            formVersion = FormVersion,
            nextStep
        });
    }

    // POST: api/v1/Onboarding/accept-terms
    [HttpPost("accept-terms")]
    public async Task<IActionResult> AcceptTerms()
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized(new { message = "Invalid user identity." });
        }

        if (await _consentRepository.HasAcceptedAsync(userId, TermsDocumentType))
        {
            return Ok(new { accepted = true });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await _mediator.Send(new RecordConsentCommand
        {
            UserId = userGuid,
            IpAddress = ip,
            ConsentText = TermsText,
            DocumentType = TermsDocumentType,
            FormVersion = FormVersion
        });

        return Ok(new { accepted = result.Data });
    }

    // POST: api/v1/Onboarding/accept-consent
    [HttpPost("accept-consent")]
    public async Task<IActionResult> AcceptConsent()
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized(new { message = "Invalid user identity." });
        }

        if (await _consentRepository.HasAcceptedAsync(userId, ConsentDocumentType))
        {
            return Ok(new { accepted = true });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await _mediator.Send(new RecordConsentCommand
        {
            UserId = userGuid,
            IpAddress = ip,
            ConsentText = ConsentText,
            DocumentType = ConsentDocumentType,
            FormVersion = FormVersion
        });

        return Ok(new { accepted = result.Data });
    }
}

