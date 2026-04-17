using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApi.Filters;

public class RequireDataContributorOnboardingFilter : IAsyncActionFilter
{
    private const string TermsDocumentType = "TermsOfService";
    private const string ConsentDocumentType = "DataConsent";

    private readonly IConsentRecordRepository _consentRepository;
    private readonly ICurrentUserService _currentUserService;

    public RequireDataContributorOnboardingFilter(
        IConsentRecordRepository consentRepository,
        ICurrentUserService currentUserService)
    {
        _consentRepository = consentRepository;
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        if (user.IsInRole("Admin"))
        {
            await next();
            return;
        }

        if (!user.IsInRole("Data Contributor"))
        {
            await next();
            return;
        }

        var userId = _currentUserService.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "User not authenticated" });
            return;
        }

        var termsAccepted = await _consentRepository.HasAcceptedAsync(userId, TermsDocumentType);
        var consentAccepted = await _consentRepository.HasAcceptedAsync(userId, ConsentDocumentType);

        if (!termsAccepted || !consentAccepted)
        {
            var nextStep = !termsAccepted ? "/onboarding/terms" : "/onboarding/consent";
            context.Result = new ObjectResult(new
            {
                message = "Onboarding required.",
                nextStep
            })
            { StatusCode = StatusCodes.Status428PreconditionRequired };
            return;
        }

        await next();
    }
}

