using Clients.Application.Abstractions;
using Clients.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clients.Infrastructure.Persistence;

public sealed class ClientsDbContext : DbContext, IClientsUnitOfWork
{
    public ClientsDbContext(DbContextOptions<ClientsDbContext> options) : base(options) { }

    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Persona>(entity =>
        {
            entity.ToTable("personas", table =>
                table.HasCheckConstraint("ck_personas_identificacion_formato", "identificacion ~ '^[0-9]{10}$'"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Genero).HasColumnName("genero").HasMaxLength(30).IsRequired();
            entity.Property(x => x.Edad).HasColumnName("edad").IsRequired();
            entity.Property(x => x.Identificacion).HasColumnName("identificacion").HasMaxLength(10).IsRequired();
            entity.HasIndex(x => x.Identificacion).IsUnique().HasDatabaseName("ux_personas_identificacion");
            entity.Property(x => x.Direccion).HasColumnName("direccion").HasMaxLength(250).IsRequired();
            entity.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("clientes");
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OccurredOnUtc).HasColumnName("occurred_on_utc").IsRequired();
            entity.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(200).IsRequired();
            entity.Property(x => x.RoutingKey).HasColumnName("routing_key").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.ProcessedOnUtc).HasColumnName("processed_on_utc");
            entity.Property(x => x.Attempts).HasColumnName("attempts").IsRequired();
            entity.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(2000);
            entity.HasIndex(x => new { x.ProcessedOnUtc, x.OccurredOnUtc }).HasDatabaseName("ix_outbox_pending");
        });
    }
}
