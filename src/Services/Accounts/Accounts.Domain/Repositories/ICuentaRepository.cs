using Accounts.Domain.Entities;

namespace Accounts.Domain.Repositories;

public interface ICuentaRepository
{
    Task<IReadOnlyCollection<Cuenta>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Cuenta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Cuenta?> GetByNumeroAsync(string numeroCuenta, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Cuenta>> GetByClienteIdAsync(Guid clienteId, bool tracking = false, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNumeroAsync(string numeroCuenta, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Cuenta cuenta, CancellationToken cancellationToken = default);
}
