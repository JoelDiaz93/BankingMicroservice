namespace Accounts.Application.Abstractions;

public interface ITransactionRunner
{
    Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
