using Banking.Contracts;

namespace Clients.Application.Abstractions;

public interface IClientEventEnqueuer
{
    Task EnqueueAsync(ClientIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
