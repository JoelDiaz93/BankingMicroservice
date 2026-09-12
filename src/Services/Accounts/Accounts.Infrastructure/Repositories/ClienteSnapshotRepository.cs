using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;
using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounts.Infrastructure.Repositories;

public sealed class ClienteSnapshotRepository : IClienteSnapshotRepository
{
    private readonly AccountsDbContext _db;
    public ClienteSnapshotRepository(AccountsDbContext db) => _db = db;

    public Task<ClienteSnapshot?> GetByIdAsync(Guid clienteId, CancellationToken cancellationToken = default)
        => _db.ClienteSnapshots.AsNoTracking().SingleOrDefaultAsync(x => x.ClienteId == clienteId, cancellationToken);

    public Task<ClienteSnapshot?> FindByFilterAsync(string filter, CancellationToken cancellationToken = default)
    {
        if (Guid.TryParse(filter, out var id))
            return _db.ClienteSnapshots.AsNoTracking().SingleOrDefaultAsync(x => x.ClienteId == id, cancellationToken);

        var normalized = filter.Trim().ToLower();
        return _db.ClienteSnapshots.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Nombre.ToLower() == normalized, cancellationToken);
    }
}
