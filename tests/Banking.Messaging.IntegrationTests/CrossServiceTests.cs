using System.Net;
using System.Net.Http.Json;
using Accounts.Application.DTOs;
using Clients.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Banking.Messaging.IntegrationTests;

[Collection(BankingSystemCollection.Name)]
public sealed class CrossServiceTests(BankingSystemFixture fixture)
{
    private static long _identificationSequence = 1800000000;
    private static int _accountSequence = 800000;
    [Fact]
    public async Task ClientCreation_PropagatesThroughRabbitMqIntoAccountsProjection()
    {
        var identification = NextIdentification();
        var response = await fixture.ClientsClient.PostAsJsonAsync("/api/clientes", new CreateClienteRequest(
            "Rabbit Client", "Masculino", 34, identification, "Quito", "0990000001", "1234", true));
        await AssertStatusAsync(response, HttpStatusCode.Created);
        var client = await response.Content.ReadFromJsonAsync<ClienteResponse>();

        var snapshot = await fixture.WaitForSnapshotAsync(client!.ClienteId);
        await fixture.WaitForMessageAuditAsync(client.ClienteId);

        Assert.Equal(client.ClienteId, snapshot.ClienteId);
        Assert.Equal("Rabbit Client", snapshot.Nombre);
        Assert.True(snapshot.Estado);

        var outbox = await fixture.FindOutboxForClientAsync(client.ClienteId);
        Assert.NotNull(outbox);
        Assert.NotNull(outbox.ProcessedOnUtc);

        await using var accountsDb = fixture.CreateAccountsDbContext();
        Assert.Equal(1, await accountsDb.InboxMessages.CountAsync(x => x.EventId == outbox.Id));
    }

    [Fact]
    public async Task FullFlow_ClientPropagation_AllowsAccountWithdrawalAndReportAcrossServices()
    {
        var identification = NextIdentification();
        var clientResponse = await fixture.ClientsClient.PostAsJsonAsync("/api/clientes", new CreateClienteRequest(
            "Cross Service Client", "Masculino", 35, identification, "Quito", "0990000002", "1234", true));
        await AssertStatusAsync(clientResponse, HttpStatusCode.Created);
        var client = await clientResponse.Content.ReadFromJsonAsync<ClienteResponse>();
        await fixture.WaitForSnapshotAsync(client!.ClienteId);

        var accountNumber = NextAccountNumber();
        var accountResponse = await fixture.AccountsClient.PostAsJsonAsync("/api/cuentas", new CreateCuentaRequest(
            accountNumber, "Ahorros", 2000m, true, client.ClienteId));
        await AssertStatusAsync(accountResponse, HttpStatusCode.Created);
        var account = await accountResponse.Content.ReadFromJsonAsync<CuentaResponse>();

        var movementResponse = await fixture.AccountsClient.PostAsJsonAsync("/api/movimientos", new CreateMovimientoRequest(
            accountNumber, -575m, new DateTime(2026, 2, 7, 12, 0, 0, DateTimeKind.Utc)));
        await AssertStatusAsync(movementResponse, HttpStatusCode.Created);
        var movement = await movementResponse.Content.ReadFromJsonAsync<MovimientoResponse>();
        Assert.Equal(1425m, movement!.Saldo);

        var report = await fixture.AccountsClient.GetFromJsonAsync<ReporteEstadoCuentaResponse>(
            $"/api/reportes?fecha=2026-02-01,2026-02-28&cliente={Uri.EscapeDataString(client.Nombre)}");

        Assert.NotNull(report);
        var reportAccount = Assert.Single(report.Cuentas.Where(x => x.CuentaId == account!.CuentaId));
        Assert.Equal(1425m, reportAccount.SaldoDisponible);
        var reportMovement = Assert.Single(reportAccount.Movimientos);
        Assert.Equal(-575m, reportMovement.Valor);
        Assert.Equal(1425m, reportMovement.SaldoDisponible);
    }
    private static string NextIdentification()
        => Interlocked.Increment(ref _identificationSequence).ToString();

    private static string NextAccountNumber()
        => Interlocked.Increment(ref _accountSequence).ToString();

    private static async Task AssertStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode == expected)
            return;

        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail($"HTTP esperado: {(int)expected} {expected}; recibido: {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
    }

}
