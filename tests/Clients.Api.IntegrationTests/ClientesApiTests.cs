using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Clients.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clients.Api.IntegrationTests;

[Collection(ClientsApiCollection.Name)]
public sealed class ClientesApiTests(ClientsApiFixture fixture)
{
    [Fact]
    public async Task Post_PersistsClientHashedPasswordAndOutboxEvent()
    {
        var identification = UniqueIdentification();
        const string rawPassword = "secret-123";

        var response = await fixture.Client.PostAsJsonAsync("/api/clientes", Request(identification, rawPassword));

        await AssertStatusAsync(response, HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ClienteResponse>();
        Assert.NotNull(created);

        await using var db = fixture.CreateDbContext();
        var stored = await db.Clientes.SingleAsync(x => x.Id == created!.ClienteId);
        Assert.NotEqual(rawPassword, stored.PasswordHash);
        Assert.StartsWith("pbkdf2-sha256$", stored.PasswordHash);
        var candidates = await db.OutboxMessages.AsNoTracking()
            .Where(x => x.ProcessedOnUtc == null && x.RoutingKey == "client.created.v1")
            .ToListAsync();
        var outbox = Assert.Single(candidates.Where(x => PayloadReferencesClient(x.Payload, created.ClienteId)));
        Assert.Equal(0, outbox.Attempts);
    }

    [Fact]
    public async Task GetById_ReturnsPreviouslyCreatedClient()
    {
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/clientes", Request(UniqueIdentification(), "1234"));
        await AssertStatusAsync(createResponse, HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ClienteResponse>();

        var response = await fixture.Client.GetAsync($"/api/clientes/{created!.ClienteId}");

        await AssertStatusAsync(response, HttpStatusCode.OK);
        var fetched = await response.Content.ReadFromJsonAsync<ClienteResponse>();
        Assert.Equal(created.ClienteId, fetched!.ClienteId);
        Assert.Equal(created.Identificacion, fetched.Identificacion);
    }

    [Fact]
    public async Task PatchEstado_PersistsInactiveStatus()
    {
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/clientes", Request(UniqueIdentification(), "1234"));
        await AssertStatusAsync(createResponse, HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ClienteResponse>();

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/clientes/{created!.ClienteId}/estado")
        {
            Content = JsonContent.Create(new PatchClienteEstadoRequest(false))
        };
        var response = await fixture.Client.SendAsync(request);

        await AssertStatusAsync(response, HttpStatusCode.OK);
        await using var db = fixture.CreateDbContext();
        var stored = await db.Clientes.AsNoTracking().SingleAsync(x => x.Id == created.ClienteId);
        Assert.False(stored.Estado);
    }

    [Fact]
    public async Task Post_DuplicateIdentification_Returns422ProblemDetails()
    {
        var identification = UniqueIdentification();
        var first = await fixture.Client.PostAsJsonAsync("/api/clientes", Request(identification, "1234"));
        await AssertStatusAsync(first, HttpStatusCode.Created);

        var second = await fixture.Client.PostAsJsonAsync("/api/clientes", Request(identification, "5678"));

        await AssertStatusAsync(second, HttpStatusCode.UnprocessableEntity);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Ya existe un cliente con la identificación indicada.", problem!.Detail);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }


    [Fact]
    public async Task Post_InvalidIdentificationFormat_Returns400()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/clientes",
            Request("12345A7890", "1234"));

        await AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_IdentificationWithInvalidLength_Returns400()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/clientes",
            Request("123456789", "1234"));

        await AssertStatusAsync(response, HttpStatusCode.BadRequest);
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

    private static async Task AssertStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode == expected)
            return;

        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail($"HTTP esperado: {(int)expected} {expected}; recibido: {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
    }

    private static CreateClienteRequest Request(string identification, string password) => new(
        "Integration Client", "Masculino", 33, identification, "Quito", "0999999999", password, true);

    private static long _identificationSequence = 1700000000;

    private static string UniqueIdentification()
        => Interlocked.Increment(ref _identificationSequence).ToString();
}
