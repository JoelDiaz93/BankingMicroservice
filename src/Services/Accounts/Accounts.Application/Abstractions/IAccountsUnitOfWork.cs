namespace Accounts.Application.Abstractions;

public interface IAccountsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
