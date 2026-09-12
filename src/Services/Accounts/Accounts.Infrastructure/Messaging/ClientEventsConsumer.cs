using System.Text;
using System.Text.Json;
using Accounts.Domain.Entities;
using Accounts.Infrastructure.Persistence;
using Banking.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Accounts.Infrastructure.Messaging;

public sealed class ClientEventsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<ClientEventsConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public ClientEventsConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<ClientEventsConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Consumo RabbitMQ deshabilitado por configuración.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                EnsureConnection();
                var consumer = new AsyncEventingBasicConsumer(_channel!);
                consumer.Received += OnMessageAsync;
                _channel!.BasicConsume(_options.Queue, autoAck: false, consumer);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el consumidor de eventos de cliente; se reintentará.");
                DisposeBrokerObjects();
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private void EnsureConnection()
    {
        if (_connection?.IsOpen == true && _channel?.IsOpen == true)
            return;

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection("accounts-client-consumer");
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.QueueDeclare(_options.Queue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_options.Queue, _options.Exchange, "client.*.v1");
        _channel.BasicQos(0, 10, global: false);
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
            var eventId = Guid.TryParse(ea.BasicProperties.MessageId, out var parsed) ? parsed : Guid.Empty;
            if (eventId == Guid.Empty)
                throw new InvalidOperationException("El evento no contiene un MessageId válido.");

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();

            if (await db.InboxMessages.AsNoTracking().AnyAsync(x => x.EventId == eventId))
            {
                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            await using var tx = await db.Database.BeginTransactionAsync();
            var integrationEvent = Deserialize(ea.RoutingKey, payload);
            var snapshot = await db.ClienteSnapshots.SingleOrDefaultAsync(x => x.ClienteId == integrationEvent.ClienteId);

            if (snapshot is null)
            {
                snapshot = new ClienteSnapshot(
                    integrationEvent.ClienteId,
                    integrationEvent.Nombre,
                    integrationEvent.Estado,
                    integrationEvent.OccurredOnUtc);
                await db.ClienteSnapshots.AddAsync(snapshot);
            }
            else
            {
                snapshot.Apply(integrationEvent.Nombre, integrationEvent.Estado, integrationEvent.OccurredOnUtc);
            }

            await db.InboxMessages.AddAsync(new InboxMessage { EventId = eventId, ReceivedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            _channel!.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando evento {RoutingKey}.", ea.RoutingKey);
            _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private static ClientIntegrationEvent Deserialize(string routingKey, string payload)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return routingKey switch
        {
            "client.created.v1" => JsonSerializer.Deserialize<ClientCreatedV1>(payload, options)!,
            "client.updated.v1" => JsonSerializer.Deserialize<ClientUpdatedV1>(payload, options)!,
            "client.deleted.v1" => JsonSerializer.Deserialize<ClientDeletedV1>(payload, options)!,
            _ => throw new InvalidOperationException($"Routing key no soportada: {routingKey}")
        };
    }

    public override void Dispose()
    {
        DisposeBrokerObjects();
        base.Dispose();
    }

    private void DisposeBrokerObjects()
    {
        try { _channel?.Dispose(); } catch { }
        try { _connection?.Dispose(); } catch { }
        _channel = null;
        _connection = null;
    }
}
