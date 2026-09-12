using Accounts.Domain.Entities;

namespace Accounts.Domain.Repositories;

public interface IClienteSnapshotRepository
{
    Task<ClienteSnapshot?> GetByIdAsync(Guid clienteId, CancellationToken cancellationToken = default);
    Task<ClienteSnapshot?> FindByFilterAsync(string filter, CancellationToken cancellationToken = default);
}
