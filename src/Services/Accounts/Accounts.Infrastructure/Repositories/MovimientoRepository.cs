using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;
using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounts.Infrastructure.Repositories;

public sealed class MovimientoRepository : IMovimientoRepository
{
    private readonly AccountsDbContext _db;
    public MovimientoRepository(AccountsDbContext db) => _db = db;

    public async Task<IReadOnlyCollection<Movimiento>> GetAllAsync(
        string? numeroCuenta = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Movimiento> query = _db.Movimientos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(numeroCuenta))
        {
            var number = numeroCuenta.Trim();
            query = query.Where(m => _db.Cuentas.Any(c => c.Id == m.CuentaId && c.NumeroCuenta == number));
        }
        if (desde.HasValue) query = query.Where(x => x.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(x => x.Fecha <= hasta.Value);
        return await query.OrderBy(x => x.Fecha).ThenBy(x => x.Id).ToListAsync(cancellationToken);
    }

    public Task<Movimiento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Movimientos.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Movimiento>> GetByCuentaIdAsync(Guid cuentaId, bool tracking = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Movimiento> query = _db.Movimientos.Where(x => x.CuentaId == cuentaId);
        if (!tracking) query = query.AsNoTracking();
        return await query.OrderBy(x => x.Fecha).ThenBy(x => x.Id).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Movimiento>> GetByCuentaIdsAndRangeAsync(
        IEnumerable<Guid> cuentaIds,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken = default)
    {
        var ids = cuentaIds.Distinct().ToArray();
        return await _db.Movimientos.AsNoTracking()
            .Where(x => ids.Contains(x.CuentaId) && x.Fecha >= desde && x.Fecha <= hasta)
            .OrderBy(x => x.Fecha).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Movimiento movimiento, CancellationToken cancellationToken = default)
        => _db.Movimientos.AddAsync(movimiento, cancellationToken).AsTask();
}
