using Banking.Contracts;
using Clients.Application.Abstractions;
using Clients.Application.DTOs;
using Clients.Domain.Entities;
using Clients.Domain.Exceptions;
using Clients.Domain.Repositories;

namespace Clients.Application.Services;

public sealed class ClienteService
{
    private readonly IClienteRepository _repository;
    private readonly IClientsUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClientEventEnqueuer _eventEnqueuer;

    public ClienteService(
        IClienteRepository repository,
        IClientsUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IClientEventEnqueuer eventEnqueuer)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _eventEnqueuer = eventEnqueuer;
    }

    public async Task<IReadOnlyCollection<ClienteResponse>> GetAllAsync(CancellationToken ct = default)
        => (await _repository.GetAllAsync(ct)).Select(Map).ToArray();

    public async Task<ClienteResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cliente = await _repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");
        return Map(cliente);
    }

    public async Task<ClienteResponse> CreateAsync(CreateClienteRequest request, CancellationToken ct = default)
    {
        ValidatePassword(request.Contrasena);
        if (await _repository.ExistsByIdentificationAsync(request.Identificacion, null, ct))
            throw new DomainException("Ya existe un cliente con la identificación indicada.");

        var cliente = Cliente.Create(
            request.Nombre,
            request.Genero,
            request.Edad,
            request.Identificacion,
            request.Direccion,
            request.Telefono,
            _passwordHasher.Hash(request.Contrasena),
            request.Estado);

        await _repository.AddAsync(cliente, ct);
        await _eventEnqueuer.EnqueueAsync(
            new ClientCreatedV1(Guid.NewGuid(), DateTime.UtcNow, cliente.Id, cliente.Nombre, cliente.Estado), ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(cliente);
    }

    public async Task<ClienteResponse> UpdateAsync(Guid id, UpdateClienteRequest request, CancellationToken ct = default)
    {
        var cliente = await _repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        if (await _repository.ExistsByIdentificationAsync(request.Identificacion, id, ct))
            throw new DomainException("Ya existe un cliente con la identificación indicada.");

        string? hash = null;
        if (!string.IsNullOrWhiteSpace(request.Contrasena))
        {
            ValidatePassword(request.Contrasena);
            hash = _passwordHasher.Hash(request.Contrasena);
        }

        cliente.Update(
            request.Nombre, request.Genero, request.Edad, request.Identificacion,
            request.Direccion, request.Telefono, hash, request.Estado);

        await _eventEnqueuer.EnqueueAsync(
            new ClientUpdatedV1(Guid.NewGuid(), DateTime.UtcNow, cliente.Id, cliente.Nombre, cliente.Estado), ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(cliente);
    }

    public async Task<ClienteResponse> PatchEstadoAsync(Guid id, bool estado, CancellationToken ct = default)
    {
        var cliente = await _repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        cliente.SetEstado(estado);
        await _eventEnqueuer.EnqueueAsync(
            new ClientUpdatedV1(Guid.NewGuid(), DateTime.UtcNow, cliente.Id, cliente.Nombre, cliente.Estado), ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(cliente);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var cliente = await _repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        await _eventEnqueuer.EnqueueAsync(
            new ClientDeletedV1(Guid.NewGuid(), DateTime.UtcNow, cliente.Id, cliente.Nombre, false), ct);
        _repository.Remove(cliente);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            throw new DomainException("La contraseña debe tener al menos 4 caracteres.");
    }

    private static ClienteResponse Map(Cliente c) => new(
        c.Id, c.Nombre, c.Genero, c.Edad, c.Identificacion,
        c.Direccion, c.Telefono, c.Estado, c.CreatedAtUtc, c.UpdatedAtUtc);
}
