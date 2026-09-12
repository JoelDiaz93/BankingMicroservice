using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Accounts.IntegrationTests;

public sealed class AccountsPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("accounts_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
    }

    public AccountsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AccountsDbContext(options);
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}

[CollectionDefinition("Accounts PostgreSQL integration")]
public sealed class AccountsPostgresCollection : ICollectionFixture<AccountsPostgreSqlFixture>
{
    public const string Name = "Accounts PostgreSQL integration";
}
