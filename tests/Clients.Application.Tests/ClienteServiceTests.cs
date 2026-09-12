using Banking.Contracts;
using Clients.Application.Abstractions;
using Clients.Application.DTOs;
using Clients.Application.Services;
using Clients.Domain.Entities;
using Clients.Domain.Exceptions;
using Clients.Domain.Repositories;
using Xunit;

namespace Clients.Application.Tests;

public sealed class ClienteServiceTests
{
    [Fact]
    public async Task CreateAsync_HashesPassword_PersistsAndEnqueuesCreatedEvent()
    {
        var repository = new FakeClienteRepository();
        var uow = new FakeUnitOfWork();
        var hasher = new FakePasswordHasher();
        var events = new FakeEventEnqueuer();
        var service = new ClienteService(repository, uow, hasher, events);

        var result = await service.CreateAsync(new CreateClienteRequest(
            "Jose Lema", "Masculino", 35, "1100000001", "Otavalo", "098254785", "1234", true));

        Assert.Single(repository.Items);
        Assert.Equal("HASH::1234", repository.Items[0].PasswordHash);
        Assert.Equal(1, uow.SaveCount);
        var integrationEvent = Assert.IsType<ClientCreatedV1>(Assert.Single(events.Events));
        Assert.Equal(result.ClienteId, integrationEvent.ClienteId);
        Assert.Equal("Jose Lema", integrationEvent.Nombre);
    }

    [Fact]
    public async Task CreateAsync_DuplicateIdentification_RejectsBeforePersistence()
    {
        var existing = Cliente.Create("Existing", "Masculino", 30, "1199999999", "Quito", "099", "hash", true);
        var repository = new FakeClienteRepository(existing);
        var uow = new FakeUnitOfWork();
        var events = new FakeEventEnqueuer();
        var service = new ClienteService(repository, uow, new FakePasswordHasher(), events);

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(new CreateClienteRequest(
            "Duplicate", "Masculino", 25, "1199999999", "Quito", "098", "1234", true)));

        Assert.Equal("Ya existe un cliente con la identificación indicada.", exception.Message);
        Assert.Single(repository.Items);
        Assert.Equal(0, uow.SaveCount);
        Assert.Empty(events.Events);
    }

    [Fact]
    public async Task UpdateAsync_WithoutNewPassword_RetainsHashAndEnqueuesUpdatedEvent()
    {
        var existing = Cliente.Create("Jose Lema", "Masculino", 35, "1100000001", "Otavalo", "098", "original-hash", true);
        var repository = new FakeClienteRepository(existing);
        var uow = new FakeUnitOfWork();
        var hasher = new FakePasswordHasher();
        var events = new FakeEventEnqueuer();
        var service = new ClienteService(repository, uow, hasher, events);

        var result = await service.UpdateAsync(existing.Id, new UpdateClienteRequest(
            "Jose Actualizado", "Masculino", 36, "1100000001", "Quito", "099", null, false));

        Assert.Equal("original-hash", existing.PasswordHash);
        Assert.Equal(0, hasher.HashCount);
        Assert.False(result.Estado);
        Assert.Equal("Jose Actualizado", result.Nombre);
        Assert.Equal(1, uow.SaveCount);
        var integrationEvent = Assert.IsType<ClientUpdatedV1>(Assert.Single(events.Events));
        Assert.Equal(existing.Id, integrationEvent.ClienteId);
        Assert.False(integrationEvent.Estado);
    }

    [Fact]
    public async Task DeleteAsync_RemovesClient_SavesAndEnqueuesDeletedEvent()
    {
        var existing = Cliente.Create("Jose Lema", "Masculino", 35, "1100000001", "Otavalo", "098", "hash", true);
        var repository = new FakeClienteRepository(existing);
        var uow = new FakeUnitOfWork();
        var events = new FakeEventEnqueuer();
        var service = new ClienteService(repository, uow, new FakePasswordHasher(), events);

        await service.DeleteAsync(existing.Id);

        Assert.Empty(repository.Items);
        Assert.Equal(1, uow.SaveCount);
        var integrationEvent = Assert.IsType<ClientDeletedV1>(Assert.Single(events.Events));
        Assert.Equal(existing.Id, integrationEvent.ClienteId);
        Assert.False(integrationEvent.Estado);
    }

    private sealed class FakeClienteRepository : IClienteRepository
    {
        public List<Cliente> Items { get; } = new();

        public FakeClienteRepository(params Cliente[] clientes) => Items.AddRange(clientes);

        public Task<IReadOnlyCollection<Cliente>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Cliente>>(Items.ToArray());

        public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

        public Task<bool> ExistsByIdentificationAsync(string identificacion, Guid? excludingId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.Any(x => x.Identificacion == identificacion.Trim() && (!excludingId.HasValue || x.Id != excludingId.Value)));

        public Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            Items.Add(cliente);
            return Task.CompletedTask;
        }

        public void Remove(Cliente cliente) => Items.Remove(cliente);
    }

    private sealed class FakeUnitOfWork : IClientsUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public int HashCount { get; private set; }
        public string Hash(string password)
        {
            HashCount++;
            return $"HASH::{password}";
        }
    }

    private sealed class FakeEventEnqueuer : IClientEventEnqueuer
    {
        public List<ClientIntegrationEvent> Events { get; } = new();
        public Task EnqueueAsync(ClientIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}
