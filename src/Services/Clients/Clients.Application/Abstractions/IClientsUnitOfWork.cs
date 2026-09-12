namespace Clients.Application.Abstractions;

public interface IClientsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
