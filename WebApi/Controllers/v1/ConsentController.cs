using Application.Features.ConsentRecords.Commands.RecordConsent;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class ConsentController : ControllerBase
{
    private readonly ISender _mediator;

    public ConsentController(ISender mediator)
    {
        _mediator = mediator;
    }

    // POST: api/v1/Consent
    [HttpPost]
    public async Task<IActionResult> Post(RecordConsentCommand command)
    {
        return Ok(await _mediator.Send(command));
    }
}
