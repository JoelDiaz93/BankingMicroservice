using Clients.Domain.Entities;

namespace Clients.Domain.Repositories;

public interface IClienteRepository
{
    Task<IReadOnlyCollection<Cliente>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdentificationAsync(string identificacion, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default);
    void Remove(Cliente cliente);
}
