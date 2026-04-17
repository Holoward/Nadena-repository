using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;
using Application.DTOs;

namespace Application.Features.Clientes.Queries.GetAllClientes;

public class GetAllClientesQuery : IRequest<ServiceResponse<IEnumerable<ClienteDto>>>
{
    // Can add pagination parameters here later if needed
}

public class GetAllClientesQueryHandler : IRequestHandler<GetAllClientesQuery, ServiceResponse<IEnumerable<ClienteDto>>>
{
    private readonly IRepositoryAsync<Client> _repositoryAsync;

    public GetAllClientesQueryHandler(IRepositoryAsync<Client> repositoryAsync)
    {
        _repositoryAsync = repositoryAsync;
    }

    public async Task<ServiceResponse<IEnumerable<ClienteDto>>> Handle(GetAllClientesQuery request, CancellationToken cancellationToken)
    {
        var clientes = await _repositoryAsync.ListAsync(cancellationToken);
        var clientesDto = clientes.Select(c => new ClienteDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Apellido = c.Apellido,
            FechaNacimiento = c.FechaNacimiento,
            Telefono = c.Telefono,
            Email = c.Email,
            Direction = c.Direction,
            Edad = c.Edad
        });
        return new ServiceResponse<IEnumerable<ClienteDto>>(clientesDto);
    }
}
