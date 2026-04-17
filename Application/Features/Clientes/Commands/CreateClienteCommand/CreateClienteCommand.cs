using System.Text.RegularExpressions;
using Application.Exceptions;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;

namespace Application.Features.Clientes.Commands.CreateClienteCommand;

public class CreateClienteCommand : IRequest<ServiceResponse<int>> 
{
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string Telefono { get; set; }
    public string Email { get; set; }
    public string Direction { get; set; }
}

public class CreateClienteCommandHandler : IRequestHandler<CreateClienteCommand, ServiceResponse<int>>
{
    private readonly IRepositoryAsync<Client> _repositoryAsync;

    public CreateClienteCommandHandler(IRepositoryAsync<Client> repositoryAsync)
    {
        _repositoryAsync = repositoryAsync;
    }

    private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<ServiceResponse<int>> Handle(CreateClienteCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ApiException("El nombre es requerido.");
        if (string.IsNullOrWhiteSpace(request.Apellido))
            throw new ApiException("El apellido es requerido.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ApiException("El email es requerido.");
        if (!EmailRegex.IsMatch(request.Email))
            throw new ApiException("El email no es válido.");

        var nuevoRegistro = new Client
        {
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            FechaNacimiento = request.FechaNacimiento,
            Telefono = request.Telefono,
            Email = request.Email,
            Direction = request.Direction
        };
        var data = await _repositoryAsync.AddAsync(nuevoRegistro, cancellationToken);
        return new ServiceResponse<int>(data.Id);
    }
}