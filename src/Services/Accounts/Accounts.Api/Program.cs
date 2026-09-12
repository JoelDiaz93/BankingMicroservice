using Accounts.Api.Middleware;
using Accounts.Application.Services;
using Accounts.Infrastructure;
using Accounts.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Cuentas y Movimientos API", Version = "v1" });
});

builder.Services.AddScoped<CuentaService>();
builder.Services.AddScoped<MovimientoService>();
builder.Services.AddScoped<ReporteService>();
builder.Services.AddAccountsInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "cuentas" }));
app.MapGet("/health/ready", async (AccountsDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ready", database = "connected" })
        : Results.Problem("Base de datos no disponible", statusCode: StatusCodes.Status503ServiceUnavailable));

app.Run();

public partial class Program { }
