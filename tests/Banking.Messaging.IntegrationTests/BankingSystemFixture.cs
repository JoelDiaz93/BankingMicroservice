using System.Text.Json;
using Accounts.Api;
using Accounts.Domain.Entities;
using Accounts.Infrastructure.Persistence;
using AccountsRabbitMqOptions = Accounts.Infrastructure.Messaging.RabbitMqOptions;
using Clients.Api;
using Clients.Infrastructure.Persistence;
using ClientsRabbitMqOptions = Clients.Infrastructure.Messaging.RabbitMqOptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Banking.Messaging.IntegrationTests;

public sealed class BankingSystemFixture : IAsyncLifetime
{
    private const string BrokerUser = "banking";
    private const string BrokerPassword = "banking";
    private const string Exchange = "banking.clients";
    private const string Queue = "accounts.client-events";

    private readonly PostgreSqlContainer _clientsPostgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("clients_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly PostgreSqlContainer _accountsPostgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("accounts_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3.13-management-alpine")
        .WithUsername(BrokerUser)
        .WithPassword(BrokerPassword)
        .Build();

    private ClientsApiFactory? _clientsFactory;
    private AccountsApiFactory? _accountsFactory;

    public HttpClient ClientsClient { get; private set; } = null!;
    public HttpClient AccountsClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _clientsPostgres.StartAsync(),
            _accountsPostgres.StartAsync(),
            _rabbitMq.StartAsync());

        await using (var clientsDb = CreateClientsDbContext())
        {
            await clientsDb.Database.EnsureCreatedAsync();
        }

        await using (var accountsDb = CreateAccountsDbContext())
        {
            await accountsDb.Database.EnsureCreatedAsync();
        }

        var broker = new BrokerSettings(
            _rabbitMq.Hostname,
            _rabbitMq.GetMappedPublicPort(5672),
            BrokerUser,
            BrokerPassword,
            Exchange,
            Queue);

        DeclareBrokerTopology(broker);

        // Start Accounts first so its durable queue/binding exists before Clients can publish.
        _accountsFactory = new AccountsApiFactory(_accountsPostgres.GetConnectionString(), broker);
        AccountsClient = _accountsFactory.CreateClient();
        await ValidateAccountsHostAsync(_accountsFactory, _accountsPostgres.GetConnectionString(), broker);

        _clientsFactory = new ClientsApiFactory(_clientsPostgres.GetConnectionString(), broker);
        ClientsClient = _clientsFactory.CreateClient();
        await ValidateClientsHostAsync(_clientsFactory, _clientsPostgres.GetConnectionString(), broker);
    }


    private static async Task ValidateClientsHostAsync(
        WebApplicationFactory<ClientsApiMarker> factory,
        string expectedConnectionString,
        BrokerSettings broker)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClientsDbContext>();
        var actualConnectionString = db.Database.GetConnectionString();
        if (!string.Equals(actualConnectionString, expectedConnectionString, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Clients cross-service host usa una conexión inesperada. Esperada: '{expectedConnectionString}'. Actual: '{actualConnectionString}'.");
        }

        if (!await db.Database.CanConnectAsync())
            throw new InvalidOperationException("Clients cross-service host no puede conectarse a PostgreSQL.");

        var rabbit = scope.ServiceProvider.GetRequiredService<IOptions<ClientsRabbitMqOptions>>().Value;
        if (!rabbit.Enabled || rabbit.HostName != broker.Host || rabbit.Port != broker.Port ||
            rabbit.UserName != broker.User || rabbit.Password != broker.Password || rabbit.Exchange != broker.Exchange)
        {
            throw new InvalidOperationException("Clients cross-service host no recibió la configuración RabbitMQ del Testcontainer.");
        }
    }

    private static async Task ValidateAccountsHostAsync(
        WebApplicationFactory<AccountsApiMarker> factory,
        string expectedConnectionString,
        BrokerSettings broker)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        var actualConnectionString = db.Database.GetConnectionString();
        if (!string.Equals(actualConnectionString, expectedConnectionString, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Accounts cross-service host usa una conexión inesperada. Esperada: '{expectedConnectionString}'. Actual: '{actualConnectionString}'.");
        }

        if (!await db.Database.CanConnectAsync())
            throw new InvalidOperationException("Accounts cross-service host no puede conectarse a PostgreSQL.");

        var rabbit = scope.ServiceProvider.GetRequiredService<IOptions<AccountsRabbitMqOptions>>().Value;
        if (!rabbit.Enabled || rabbit.HostName != broker.Host || rabbit.Port != broker.Port ||
            rabbit.UserName != broker.User || rabbit.Password != broker.Password ||
            rabbit.Exchange != broker.Exchange || rabbit.Queue != broker.Queue)
        {
            throw new InvalidOperationException("Accounts cross-service host no recibió la configuración RabbitMQ del Testcontainer.");
        }
    }

    public ClientsDbContext CreateClientsDbContext()
    {
        var options = new DbContextOptionsBuilder<ClientsDbContext>()
            .UseNpgsql(_clientsPostgres.GetConnectionString())
            .Options;
        return new ClientsDbContext(options);
    }

    public AccountsDbContext CreateAccountsDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseNpgsql(_accountsPostgres.GetConnectionString())
            .Options;
        return new AccountsDbContext(options);
    }

    public async Task<ClienteSnapshot> WaitForSnapshotAsync(Guid clienteId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(25));
        while (DateTime.UtcNow < deadline)
        {
            await using var db = CreateAccountsDbContext();
            var snapshot = await db.ClienteSnapshots.AsNoTracking().SingleOrDefaultAsync(x => x.ClienteId == clienteId);
            if (snapshot is not null)
                return snapshot;
            await Task.Delay(200);
        }

        throw new TimeoutException($"El cliente {clienteId} no se propagó a Accounts dentro del tiempo esperado.");
    }

    public async Task<OutboxMessage?> FindOutboxForClientAsync(
        Guid clienteId,
        string routingKey = "client.created.v1")
    {
        await using var clientsDb = CreateClientsDbContext();
        var candidates = await clientsDb.OutboxMessages.AsNoTracking()
            .Where(x => x.RoutingKey == routingKey)
            .OrderByDescending(x => x.OccurredOnUtc)
            .Take(100)
            .ToListAsync();

        return candidates.SingleOrDefault(x => PayloadReferencesClient(x.Payload, clienteId));
    }

    public async Task WaitForMessageAuditAsync(Guid clienteId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(25));
        while (DateTime.UtcNow < deadline)
        {
            var outbox = await FindOutboxForClientAsync(clienteId);

            if (outbox?.ProcessedOnUtc is not null)
            {
                await using var accountsDb = CreateAccountsDbContext();
                if (await accountsDb.InboxMessages.AsNoTracking().AnyAsync(x => x.EventId == outbox.Id))
                    return;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"No se confirmó Outbox/Inbox para el cliente {clienteId}.");
    }

    private static bool PayloadReferencesClient(string payload, Guid clienteId)
    {
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("ClienteId", out var clienteIdElement))
            return false;

        return clienteIdElement.ValueKind == JsonValueKind.String
            && Guid.TryParse(clienteIdElement.GetString(), out var parsed)
            && parsed == clienteId;
    }


    private static void DeclareBrokerTopology(BrokerSettings broker)
    {
        var factory = new ConnectionFactory
        {
            HostName = broker.Host,
            Port = broker.Port,
            UserName = broker.User,
            Password = broker.Password
        };

        using var connection = factory.CreateConnection("banking-cross-service-tests");
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(broker.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.QueueDeclare(broker.Queue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(broker.Queue, broker.Exchange, "client.*.v1");
    }

    public async Task DisposeAsync()
    {
        ClientsClient?.Dispose();
        AccountsClient?.Dispose();
        _clientsFactory?.Dispose();
        _accountsFactory?.Dispose();
        await _clientsPostgres.DisposeAsync();
        await _accountsPostgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    private sealed record BrokerSettings(
        string Host,
        int Port,
        string User,
        string Password,
        string Exchange,
        string Queue);

    private sealed class ClientsApiFactory(string connectionString, BrokerSettings broker) : WebApplicationFactory<ClientsApiMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<ClientsDbContext>>();
                services.AddSingleton(new DbContextOptionsBuilder<ClientsDbContext>()
                    .UseNpgsql(connectionString)
                    .Options);

                services.PostConfigure<ClientsRabbitMqOptions>(options =>
                {
                    options.Enabled = true;
                    options.HostName = broker.Host;
                    options.Port = broker.Port;
                    options.UserName = broker.User;
                    options.Password = broker.Password;
                    options.Exchange = broker.Exchange;
                });
            });
        }
    }

    private sealed class AccountsApiFactory(string connectionString, BrokerSettings broker) : WebApplicationFactory<AccountsApiMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<AccountsDbContext>>();
                services.AddSingleton(new DbContextOptionsBuilder<AccountsDbContext>()
                    .UseNpgsql(connectionString)
                    .Options);

                services.PostConfigure<AccountsRabbitMqOptions>(options =>
                {
                    options.Enabled = true;
                    options.HostName = broker.Host;
                    options.Port = broker.Port;
                    options.UserName = broker.User;
                    options.Password = broker.Password;
                    options.Exchange = broker.Exchange;
                    options.Queue = broker.Queue;
                });
            });
        }
    }

}

[CollectionDefinition("Banking cross-service integration", DisableParallelization = true)]
public sealed class BankingSystemCollection : ICollectionFixture<BankingSystemFixture>
{
    public const string Name = "Banking cross-service integration";
}
