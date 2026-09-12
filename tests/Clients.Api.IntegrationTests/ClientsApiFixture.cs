using Clients.Api;
using Clients.Infrastructure.Messaging;
using Clients.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace Clients.Api.IntegrationTests;

public sealed class ClientsApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("clients_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private ClientsApiFactory? _factory;

    public string ConnectionString => _postgres.GetConnectionString();
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using (var db = CreateDbContext())
        {
            await db.Database.EnsureCreatedAsync();
        }

        _factory = new ClientsApiFactory(ConnectionString);
        Client = _factory.CreateClient();

        // Fail early with a precise diagnostic if the test host did not receive
        // the PostgreSQL Testcontainer connection. This avoids opaque HTTP 500s.
        using var scope = _factory.Services.CreateScope();
        var hostDb = scope.ServiceProvider.GetRequiredService<ClientsDbContext>();
        var configured = hostDb.Database.GetConnectionString();
        if (!string.Equals(configured, ConnectionString, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Clients test host usa una conexión inesperada. Esperada: '{ConnectionString}'. Actual: '{configured}'.");
        }

        if (!await hostDb.Database.CanConnectAsync())
            throw new InvalidOperationException("Clients test host no puede conectarse al PostgreSQL de Testcontainers.");
    }

    public ClientsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ClientsDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new ClientsDbContext(options);
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        _factory?.Dispose();
        await _postgres.DisposeAsync();
    }

    private sealed class ClientsApiFactory(string connectionString) : WebApplicationFactory<ClientsApiMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Program.cs captures the appsettings connection string while registering
                // AddDbContext. Replace the resolved options after application services are
                // registered so API requests use the PostgreSQL Testcontainer deterministically.
                services.RemoveAll<DbContextOptions<ClientsDbContext>>();
                services.AddSingleton(new DbContextOptionsBuilder<ClientsDbContext>()
                    .UseNpgsql(connectionString)
                    .Options);

                // Keep the outbox persisted but disable the publisher in this API-only suite.
                services.PostConfigure<RabbitMqOptions>(options => options.Enabled = false);
            });
        }
    }
}

[CollectionDefinition("Clients API integration", DisableParallelization = true)]
public sealed class ClientsApiCollection : ICollectionFixture<ClientsApiFixture>
{
    public const string Name = "Clients API integration";
}
