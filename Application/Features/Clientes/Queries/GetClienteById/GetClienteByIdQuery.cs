using Application.Exceptions;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;
using Application.DTOs;

namespace Application.Features.Clientes.Queries.GetClienteById;

public class GetClienteByIdQuery : IRequest<ServiceResponse<ClienteDto>>
{
    public int Id { get; set; }
}

public class GetClienteByIdQueryHandler : IRequestHandler<GetClienteByIdQuery, ServiceResponse<ClienteDto>>
{
    private readonly IRepositoryAsync<Client> _repositoryAsync;

    public GetClienteByIdQueryHandler(IRepositoryAsync<Client> repositoryAsync)
    {
        _repositoryAsync = repositoryAsync;
    }

    public async Task<ServiceResponse<ClienteDto>> Handle(GetClienteByIdQuery request, CancellationToken cancellationToken)
    {
        var cliente = await _repositoryAsync.GetByIdAsync(request.Id);
        if (cliente == null)
        {
            throw new Exceptions.ApiException($"Client Not Found.");
        }
        var clienteDto = new ClienteDto
        {
            Id = cliente.Id,
            Nombre = cliente.Nombre,
            Apellido = cliente.Apellido,
            FechaNacimiento = cliente.FechaNacimiento,
            Telefono = cliente.Telefono,
            Email = cliente.Email,
            Direction = cliente.Direction,
            Edad = cliente.Edad
        };
        return new ServiceResponse<ClienteDto>(clienteDto);
    }
}
