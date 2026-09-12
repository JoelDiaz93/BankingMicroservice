using System.Text;
using Clients.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Clients.Infrastructure.Messaging;

public sealed class RabbitMqOutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqOutboxPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqOutboxPublisher(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqOutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Publicación RabbitMQ deshabilitada por configuración.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                EnsureConnection();
                await PublishBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando el outbox de clientes; se reintentará.");
                DisposeBrokerObjects();
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
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
            AutomaticRecoveryEnabled = true
        };

        _connection = factory.CreateConnection("clients-outbox-publisher");
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.ConfirmSelect();
    }

    private async Task PublishBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClientsDbContext>();

        var messages = await db.OutboxMessages
            .Where(x => x.ProcessedOnUtc == null)
            .OrderBy(x => x.OccurredOnUtc)
            .Take(50)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(message.Payload);
                var props = _channel!.CreateBasicProperties();
                props.Persistent = true;
                props.MessageId = message.Id.ToString();
                props.Type = message.EventType;
                props.ContentType = "application/json";

                _channel.BasicPublish(_options.Exchange, message.RoutingKey, props, body);
                _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Attempts++;
                message.LastError = null;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.Message[..Math.Min(ex.Message.Length, 2000)];
                _logger.LogWarning(ex, "No se pudo publicar el mensaje de outbox {MessageId}.", message.Id);
                break;
            }
        }

        await db.SaveChangesAsync(ct);
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
