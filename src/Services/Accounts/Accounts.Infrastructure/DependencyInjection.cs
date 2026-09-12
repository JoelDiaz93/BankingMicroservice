using Accounts.Application.Abstractions;
using Accounts.Domain.Repositories;
using Accounts.Infrastructure.Messaging;
using Accounts.Infrastructure.Persistence;
using Accounts.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AccountsDb")
            ?? throw new InvalidOperationException("No se configuró ConnectionStrings:AccountsDb.");

        services.AddDbContext<AccountsDbContext>(options => options.UseNpgsql(connectionString));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddScoped<ICuentaRepository, CuentaRepository>();
        services.AddScoped<IMovimientoRepository, MovimientoRepository>();
        services.AddScoped<IClienteSnapshotRepository, ClienteSnapshotRepository>();
        services.AddScoped<IAccountsUnitOfWork>(sp => sp.GetRequiredService<AccountsDbContext>());
        services.AddScoped<ITransactionRunner, EfTransactionRunner>();
        services.AddHostedService<ClientEventsConsumer>();
        return services;
    }
}
