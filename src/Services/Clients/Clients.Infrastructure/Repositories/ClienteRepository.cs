using Clients.Domain.Entities;
using Clients.Domain.Repositories;
using Clients.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clients.Infrastructure.Repositories;

public sealed class ClienteRepository : IClienteRepository
{
    private readonly ClientsDbContext _db;

    public ClienteRepository(ClientsDbContext db) => _db = db;

    public async Task<IReadOnlyCollection<Cliente>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Clientes.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(cancellationToken);

    public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Clientes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByIdentificationAsync(
        string identificacion,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = identificacion.Trim();
        return _db.Clientes.AnyAsync(
            x => x.Identificacion == normalized && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
        => _db.Clientes.AddAsync(cliente, cancellationToken).AsTask();

    public void Remove(Cliente cliente) => _db.Clientes.Remove(cliente);
}
