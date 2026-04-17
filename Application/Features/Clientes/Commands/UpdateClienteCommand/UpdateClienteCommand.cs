using Application.Exceptions;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;

namespace Application.Features.Clientes.Commands.UpdateClienteCommand;

public class UpdateClienteCommand : IRequest<ServiceResponse<int>>
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string Telefono { get; set; }
    public string Email { get; set; }
    public string Direction { get; set; }
}

public class UpdateClienteCommandHandler : IRequestHandler<UpdateClienteCommand, ServiceResponse<int>>
{
    private readonly IRepositoryAsync<Client> _repositoryAsync;

    public UpdateClienteCommandHandler(IRepositoryAsync<Client> repositoryAsync)
    {
        _repositoryAsync = repositoryAsync;
    }

    public async Task<ServiceResponse<int>> Handle(UpdateClienteCommand request, CancellationToken cancellationToken)
    {
        var client = await _repositoryAsync.GetByIdAsync(request.Id);
        if (client == null)
        {
            throw new Exceptions.ApiException($"Client Not Found.");
        }
        else
        {
            client.Nombre = request.Nombre;
            client.Apellido = request.Apellido;
            client.FechaNacimiento = request.FechaNacimiento;
            client.Telefono = request.Telefono;
            client.Email = request.Email;
            client.Direction = request.Direction;

            await _repositoryAsync.UpdateAsync(client);
            return new ServiceResponse<int>(client.Id);
        }
    }
}
