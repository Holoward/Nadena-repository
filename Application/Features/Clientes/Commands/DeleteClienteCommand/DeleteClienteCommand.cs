using Application.Exceptions;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;

namespace Application.Features.Clientes.Commands.DeleteClienteCommand;

public class DeleteClienteCommand : IRequest<ServiceResponse<int>>
{
    public int Id { get; set; }
}

public class DeleteClienteCommandHandler : IRequestHandler<DeleteClienteCommand, ServiceResponse<int>>
{
    private readonly IRepositoryAsync<Client> _repositoryAsync;

    public DeleteClienteCommandHandler(IRepositoryAsync<Client> repositoryAsync)
    {
        _repositoryAsync = repositoryAsync;
    }

    public async Task<ServiceResponse<int>> Handle(DeleteClienteCommand request, CancellationToken cancellationToken)
    {
        var clientToDelete = await _repositoryAsync.GetByIdAsync(request.Id);
        if (clientToDelete == null)
        {
            throw new Exceptions.ApiException($"Client Not Found.");
        }
        await _repositoryAsync.DeleteAsync(clientToDelete);
        return new ServiceResponse<int>(clientToDelete.Id);
    }
}
