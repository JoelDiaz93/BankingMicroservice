using Clients.Application.Abstractions;
using Clients.Domain.Repositories;
using Clients.Infrastructure.Messaging;
using Clients.Infrastructure.Persistence;
using Clients.Infrastructure.Repositories;
using Clients.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clients.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddClientsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ClientsDb")
            ?? throw new InvalidOperationException("No se configuró ConnectionStrings:ClientsDb.");

        services.AddDbContext<ClientsDbContext>(options => options.UseNpgsql(connectionString));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IClientsUnitOfWork>(sp => sp.GetRequiredService<ClientsDbContext>());
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IClientEventEnqueuer, OutboxEventEnqueuer>();
        services.AddHostedService<RabbitMqOutboxPublisher>();
        return services;
    }
}
