using Accounts.Domain.Enums;
using Accounts.Domain.Exceptions;

namespace Accounts.Domain.Entities;

public sealed class Cuenta
{
    private Cuenta() { }

    private Cuenta(Guid id, string numeroCuenta, TipoCuenta tipoCuenta, decimal saldoInicial, bool estado, Guid clienteId)
    {
        Id = id;
        NumeroCuenta = ValidateNumero(numeroCuenta);
        TipoCuenta = tipoCuenta;
        SetSaldoInicial(saldoInicial);
        SaldoDisponible = SaldoInicial;
        Estado = estado;
        ClienteId = clienteId == Guid.Empty ? throw new DomainException("El cliente es obligatorio.") : clienteId;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public string NumeroCuenta { get; private set; } = string.Empty;
    public TipoCuenta TipoCuenta { get; private set; }
    public decimal SaldoInicial { get; private set; }
    public decimal SaldoDisponible { get; private set; }
    public bool Estado { get; private set; }
    public Guid ClienteId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Cuenta Create(string numeroCuenta, TipoCuenta tipoCuenta, decimal saldoInicial, bool estado, Guid clienteId)
        => new(Guid.NewGuid(), numeroCuenta, tipoCuenta, saldoInicial, estado, clienteId);

    public void Update(string numeroCuenta, TipoCuenta tipoCuenta, decimal saldoInicial, bool estado)
    {
        NumeroCuenta = ValidateNumero(numeroCuenta);
        TipoCuenta = tipoCuenta;
        SetSaldoInicial(saldoInicial);
        Estado = estado;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetEstado(bool estado)
    {
        Estado = estado;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public decimal ApplyMovement(decimal valor)
    {
        if (!Estado)
            throw new DomainException("La cuenta se encuentra inactiva.");
        if (valor == 0)
            throw new DomainException("El valor del movimiento no puede ser cero.");

        var newBalance = decimal.Round(SaldoDisponible + valor, 2);
        if (newBalance < 0)
            throw new InsufficientBalanceException();

        SaldoDisponible = newBalance;
        UpdatedAtUtc = DateTime.UtcNow;
        return SaldoDisponible;
    }

    public void RecalculateBalance(IEnumerable<Movimiento> movimientos)
    {
        var running = SaldoInicial;
        foreach (var movimiento in movimientos.OrderBy(x => x.Fecha).ThenBy(x => x.Id))
        {
            running = decimal.Round(running + movimiento.Valor, 2);
            if (running < 0)
                throw new InsufficientBalanceException();
            movimiento.SetSaldo(running);
        }
        SaldoDisponible = running;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetSaldoInicial(decimal saldoInicial)
    {
        if (saldoInicial < 0)
            throw new DomainException("El saldo inicial no puede ser negativo.");
        SaldoInicial = decimal.Round(saldoInicial, 2);
    }

    private static string ValidateNumero(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("El número de cuenta es obligatorio.");

        var normalized = value.Trim();
        if (normalized.Length != 6 || !normalized.All(IsAsciiDigit))
            throw new DomainException("El número de cuenta debe contener exactamente 6 dígitos.");

        return normalized;
    }

    private static bool IsAsciiDigit(char value) => value is >= '0' and <= '9';
}
