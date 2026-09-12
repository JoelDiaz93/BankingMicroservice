using Clients.Api.Middleware;
using Clients.Application.Services;
using Clients.Infrastructure;
using Clients.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Clientes API", Version = "v1" });
});
builder.Services.AddScoped<ClienteService>();
builder.Services.AddClientsInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "clientes" }));
app.MapGet("/health/ready", async (ClientsDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ready", database = "connected" })
        : Results.Problem("Base de datos no disponible", statusCode: StatusCodes.Status503ServiceUnavailable));

app.Run();

public partial class Program { }
