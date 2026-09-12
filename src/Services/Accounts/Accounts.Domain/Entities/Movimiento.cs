using Accounts.Domain.Enums;
using Accounts.Domain.Exceptions;

namespace Accounts.Domain.Entities;

public sealed class Movimiento
{
    private Movimiento() { }

    private Movimiento(Guid id, Guid cuentaId, DateTime fecha, decimal valor, decimal saldo)
    {
        Id = id;
        CuentaId = cuentaId;
        Fecha = NormalizeUtc(fecha);
        SetValor(valor);
        Saldo = saldo;
    }

    public Guid Id { get; private set; }
    public Guid CuentaId { get; private set; }
    public DateTime Fecha { get; private set; }
    public TipoMovimiento TipoMovimiento { get; private set; }
    public decimal Valor { get; private set; }
    public decimal Saldo { get; private set; }

    public static Movimiento Create(Guid cuentaId, decimal valor, decimal saldoResultante, DateTime? fecha = null)
    {
        if (cuentaId == Guid.Empty)
            throw new DomainException("La cuenta es obligatoria.");
        return new Movimiento(Guid.NewGuid(), cuentaId, fecha ?? DateTime.UtcNow, valor, saldoResultante);
    }

    public void Update(decimal valor, DateTime fecha)
    {
        SetValor(valor);
        Fecha = NormalizeUtc(fecha);
    }

    public void SetSaldo(decimal saldo) => Saldo = decimal.Round(saldo, 2);

    private void SetValor(decimal valor)
    {
        if (valor == 0)
            throw new DomainException("El valor del movimiento no puede ser cero.");

        Valor = decimal.Round(valor, 2);
        TipoMovimiento = valor > 0 ? TipoMovimiento.Deposito : TipoMovimiento.Retiro;
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
