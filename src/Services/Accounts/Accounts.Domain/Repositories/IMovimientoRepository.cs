using Accounts.Domain.Entities;

namespace Accounts.Domain.Repositories;

public interface IMovimientoRepository
{
    Task<IReadOnlyCollection<Movimiento>> GetAllAsync(string? numeroCuenta = null, DateTime? desde = null, DateTime? hasta = null, CancellationToken cancellationToken = default);
    Task<Movimiento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Movimiento>> GetByCuentaIdAsync(Guid cuentaId, bool tracking = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Movimiento>> GetByCuentaIdsAndRangeAsync(IEnumerable<Guid> cuentaIds, DateTime desde, DateTime hasta, CancellationToken cancellationToken = default);
    Task AddAsync(Movimiento movimiento, CancellationToken cancellationToken = default);
}
