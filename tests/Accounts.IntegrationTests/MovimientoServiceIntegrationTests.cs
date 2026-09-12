using Accounts.Application.DTOs;
using Accounts.Application.Services;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Exceptions;
using Accounts.Infrastructure.Persistence;
using Accounts.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Accounts.IntegrationTests;

[Collection(AccountsPostgresCollection.Name)]
public sealed class MovimientoServiceIntegrationTests(AccountsPostgreSqlFixture fixture)
{
    private static int _accountSequence = 600000;
    [Fact]
    public async Task CreateWithdrawal_PersistsMovementAndUpdatesBalance()
    {
        await using var db = fixture.CreateDbContext();
        var account = await SeedAccountAsync(db, 2000m, "Jose Withdrawal");
        var service = MovementService(db);

        var result = await service.CreateAsync(new(account.NumeroCuenta, -575m, Utc(2026, 2, 7)));

        Assert.Equal(-575m, result.Valor);
        Assert.Equal(1425m, result.Saldo);
        Assert.Equal(1425m, (await db.Cuentas.AsNoTracking().SingleAsync(x => x.Id == account.Id)).SaldoDisponible);
        Assert.Single(await db.Movimientos.AsNoTracking().Where(x => x.CuentaId == account.Id).ToListAsync());
    }

    [Fact]
    public async Task CreateDeposit_PersistsMovementAndUpdatesBalance()
    {
        await using var db = fixture.CreateDbContext();
        var account = await SeedAccountAsync(db, 100m, "Marianela Deposit");
        var service = MovementService(db);

        var result = await service.CreateAsync(new(account.NumeroCuenta, 600m, Utc(2026, 2, 8)));

        Assert.Equal("Deposito", result.TipoMovimiento);
        Assert.Equal(700m, result.Saldo);
        Assert.Equal(700m, (await db.Cuentas.AsNoTracking().SingleAsync(x => x.Id == account.Id)).SaldoDisponible);
    }

    [Fact]
    public async Task Overdraw_RollsBackWithoutPersistingMovementOrChangingBalance()
    {
        await using var db = fixture.CreateDbContext();
        var account = await SeedAccountAsync(db, 100m, "Overdraw Client");
        var service = MovementService(db);

        var exception = await Assert.ThrowsAsync<InsufficientBalanceException>(() =>
            service.CreateAsync(new(account.NumeroCuenta, -150m, Utc(2026, 2, 9))));

        Assert.Equal("Saldo no disponible", exception.Message);
        db.ChangeTracker.Clear();
        Assert.Equal(100m, (await db.Cuentas.AsNoTracking().SingleAsync(x => x.Id == account.Id)).SaldoDisponible);
        Assert.Empty(await db.Movimientos.AsNoTracking().Where(x => x.CuentaId == account.Id).ToListAsync());
    }

    [Fact]
    public async Task Report_FiltersMovementsByClientAndDateRange()
    {
        await using var db = fixture.CreateDbContext();
        var account = await SeedAccountAsync(db, 1000m, $"Report-{Guid.NewGuid():N}");
        var movementService = MovementService(db);
        await movementService.CreateAsync(new(account.NumeroCuenta, 100m, Utc(2026, 2, 1)));
        await movementService.CreateAsync(new(account.NumeroCuenta, -50m, Utc(2026, 2, 10)));
        await movementService.CreateAsync(new(account.NumeroCuenta, 200m, Utc(2026, 3, 1)));

        var reportService = new ReporteService(
            new ClienteSnapshotRepository(db),
            new CuentaRepository(db),
            new MovimientoRepository(db));
        var client = await db.ClienteSnapshots.AsNoTracking().SingleAsync(x => x.ClienteId == account.ClienteId);

        var report = await reportService.GenerateAsync(Utc(2026, 2, 1), Utc(2026, 2, 28), client.Nombre);

        var reportAccount = Assert.Single(report.Cuentas);
        Assert.Equal(2, reportAccount.Movimientos.Count);
        Assert.All(reportAccount.Movimientos, x => Assert.Equal(2, x.Fecha.Month));
    }

    [Fact]
    public async Task ConcurrentWithdrawals_CannotBothConsumeSameBalance()
    {
        Guid accountId;
        string number;
        await using (var seedDb = fixture.CreateDbContext())
        {
            var account = await SeedAccountAsync(seedDb, 100m, "Concurrent Client");
            accountId = account.Id;
            number = account.NumeroCuenta;
        }

        async Task<Exception?> TryWithdrawAsync()
        {
            await using var db = fixture.CreateDbContext();
            try
            {
                await MovementService(db).CreateAsync(new(number, -80m, DateTime.UtcNow));
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        var outcomes = await Task.WhenAll(TryWithdrawAsync(), TryWithdrawAsync());

        Assert.Equal(1, outcomes.Count(x => x is null));
        Assert.Equal(1, outcomes.Count(x => x is not null));
        await using var verifyDb = fixture.CreateDbContext();
        Assert.Equal(20m, (await verifyDb.Cuentas.AsNoTracking().SingleAsync(x => x.Id == accountId)).SaldoDisponible);
        Assert.Single(await verifyDb.Movimientos.AsNoTracking().Where(x => x.CuentaId == accountId).ToListAsync());
    }

    private static MovimientoService MovementService(AccountsDbContext db)
        => new(new CuentaRepository(db), new MovimientoRepository(db), db, new EfTransactionRunner(db));

    private static async Task<Cuenta> SeedAccountAsync(AccountsDbContext db, decimal balance, string clientName)
    {
        var clientId = Guid.NewGuid();
        await db.ClienteSnapshots.AddAsync(new ClienteSnapshot(clientId, clientName, true, DateTime.UtcNow));
        var account = Cuenta.Create(UniqueAccountNumber(), TipoCuenta.Ahorros, balance, true, clientId);
        await db.Cuentas.AddAsync(account);
        await db.SaveChangesAsync();
        return account;
    }

    private static string UniqueAccountNumber()
        => Interlocked.Increment(ref _accountSequence).ToString();

    private static DateTime Utc(int year, int month, int day)
        => new(year, month, day, 12, 0, 0, DateTimeKind.Utc);
}
