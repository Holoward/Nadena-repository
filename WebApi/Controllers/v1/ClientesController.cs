using Application.Features.Clientes.Commands.CreateClienteCommand;
using Application.Features.Clientes.Commands.DeleteClienteCommand;
using Application.Features.Clientes.Commands.UpdateClienteCommand;
using Application.Features.Clientes.Queries.GetAllClientes;
using Application.Features.Clientes.Queries.GetClienteById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/v1/Clientes
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _mediator.Send(new GetAllClientesQuery()));
    }

    // GET: api/v1/Clientes/5
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        return Ok(await _mediator.Send(new GetClienteByIdQuery { Id = id }));
    }

    // POST: api/v1/Clientes
    [HttpPost]
    public async Task<IActionResult> Post(CreateClienteCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    // PUT: api/v1/Clientes/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, UpdateClienteCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest();
        }

        return Ok(await _mediator.Send(command));
    }

    // DELETE: api/v1/Clientes/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        return Ok(await _mediator.Send(new DeleteClienteCommand { Id = id }));
    }
}
