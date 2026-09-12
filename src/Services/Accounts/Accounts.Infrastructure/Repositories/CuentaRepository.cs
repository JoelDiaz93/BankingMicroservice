using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;
using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounts.Infrastructure.Repositories;

public sealed class CuentaRepository : ICuentaRepository
{
    private readonly AccountsDbContext _db;
    public CuentaRepository(AccountsDbContext db) => _db = db;

    public async Task<IReadOnlyCollection<Cuenta>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Cuentas.AsNoTracking().OrderBy(x => x.NumeroCuenta).ToListAsync(cancellationToken);

    public Task<Cuenta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Cuentas.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Cuenta?> GetByNumeroAsync(string numeroCuenta, CancellationToken cancellationToken = default)
        => _db.Cuentas.SingleOrDefaultAsync(x => x.NumeroCuenta == numeroCuenta.Trim(), cancellationToken);

    public async Task<IReadOnlyCollection<Cuenta>> GetByClienteIdAsync(Guid clienteId, bool tracking = false, CancellationToken cancellationToken = default)
    {
        var query = _db.Cuentas.Where(x => x.ClienteId == clienteId);
        if (!tracking) query = query.AsNoTracking();
        return await query.OrderBy(x => x.NumeroCuenta).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNumeroAsync(string numeroCuenta, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var normalized = numeroCuenta.Trim();
        return _db.Cuentas.AnyAsync(
            x => x.NumeroCuenta == normalized && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public Task AddAsync(Cuenta cuenta, CancellationToken cancellationToken = default)
        => _db.Cuentas.AddAsync(cuenta, cancellationToken).AsTask();
}
