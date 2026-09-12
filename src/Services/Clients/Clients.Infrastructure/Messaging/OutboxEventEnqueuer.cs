using System.Text.Json;
using Banking.Contracts;
using Clients.Application.Abstractions;
using Clients.Infrastructure.Persistence;

namespace Clients.Infrastructure.Messaging;

public sealed class OutboxEventEnqueuer : IClientEventEnqueuer
{
    private readonly ClientsDbContext _db;

    public OutboxEventEnqueuer(ClientsDbContext db) => _db = db;

    public Task EnqueueAsync(ClientIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var routingKey = integrationEvent switch
        {
            ClientCreatedV1 => "client.created.v1",
            ClientUpdatedV1 => "client.updated.v1",
            ClientDeletedV1 => "client.deleted.v1",
            _ => throw new InvalidOperationException($"Evento no soportado: {integrationEvent.GetType().Name}")
        };

        var message = new OutboxMessage
        {
            Id = integrationEvent.EventId,
            OccurredOnUtc = integrationEvent.OccurredOnUtc,
            EventType = integrationEvent.GetType().AssemblyQualifiedName ?? integrationEvent.GetType().FullName!,
            RoutingKey = routingKey,
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            Attempts = 0
        };

        return _db.OutboxMessages.AddAsync(message, cancellationToken).AsTask();
    }
}
