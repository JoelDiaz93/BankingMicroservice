namespace Accounts.Infrastructure.Persistence;

public sealed class InboxMessage
{
    public Guid EventId { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
}
