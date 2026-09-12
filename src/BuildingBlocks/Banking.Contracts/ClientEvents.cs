namespace Banking.Contracts;

public abstract record ClientIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid ClienteId,
    string Nombre,
    bool Estado);

public sealed record ClientCreatedV1(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid ClienteId,
    string Nombre,
    bool Estado)
    : ClientIntegrationEvent(EventId, OccurredOnUtc, ClienteId, Nombre, Estado);

public sealed record ClientUpdatedV1(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid ClienteId,
    string Nombre,
    bool Estado)
    : ClientIntegrationEvent(EventId, OccurredOnUtc, ClienteId, Nombre, Estado);

public sealed record ClientDeletedV1(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid ClienteId,
    string Nombre,
    bool Estado)
    : ClientIntegrationEvent(EventId, OccurredOnUtc, ClienteId, Nombre, Estado);
