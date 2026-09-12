using Accounts.Application.Abstractions;
using Accounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Accounts.Infrastructure.Persistence;

public sealed class AccountsDbContext : DbContext, IAccountsUnitOfWork
{
    public AccountsDbContext(DbContextOptions<AccountsDbContext> options) : base(options) { }

    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
    public DbSet<ClienteSnapshot> ClienteSnapshots => Set<ClienteSnapshot>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClienteSnapshot>(entity =>
        {
            entity.ToTable("client_snapshots");
            entity.HasKey(x => x.ClienteId);
            entity.Property(x => x.ClienteId).HasColumnName("cliente_id");
            entity.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.HasIndex(x => x.Nombre).HasDatabaseName("ix_client_snapshots_nombre");
        });

        modelBuilder.Entity<Cuenta>(entity =>
        {
            entity.ToTable("cuentas", table =>
                table.HasCheckConstraint("ck_cuentas_numero_formato", "numero_cuenta ~ '^[0-9]{6}$'"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.NumeroCuenta).HasColumnName("numero_cuenta").HasMaxLength(6).IsRequired();
            entity.HasIndex(x => x.NumeroCuenta).IsUnique().HasDatabaseName("ux_cuentas_numero");
            entity.Property(x => x.TipoCuenta).HasColumnName("tipo_cuenta").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.SaldoInicial).HasColumnName("saldo_inicial").HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.SaldoDisponible).HasColumnName("saldo_disponible").HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.ClienteId).HasColumnName("cliente_id").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.HasIndex(x => x.ClienteId).HasDatabaseName("ix_cuentas_cliente_id");
        });

        modelBuilder.Entity<Movimiento>(entity =>
        {
            entity.ToTable("movimientos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CuentaId).HasColumnName("cuenta_id").IsRequired();
            entity.Property(x => x.Fecha).HasColumnName("fecha").IsRequired();
            entity.Property(x => x.TipoMovimiento).HasColumnName("tipo_movimiento").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Valor).HasColumnName("valor").HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Saldo).HasColumnName("saldo").HasPrecision(18, 2).IsRequired();
            entity.HasOne<Cuenta>().WithMany().HasForeignKey(x => x.CuentaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.CuentaId, x.Fecha }).HasDatabaseName("ix_movimientos_cuenta_fecha");
        });

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("inbox_messages");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).HasColumnName("event_id");
            entity.Property(x => x.ReceivedAtUtc).HasColumnName("received_at_utc").IsRequired();
        });
    }
}
