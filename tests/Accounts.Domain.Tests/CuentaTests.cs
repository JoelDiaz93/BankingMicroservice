using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Exceptions;
using Xunit;

namespace Accounts.Domain.Tests;

public sealed class CuentaTests
{
    [Fact]
    public void Create_WithValidData_RoundsAndSetsAvailableBalance()
    {
        var account = Cuenta.Create("100001", TipoCuenta.Ahorros, 100.126m, true, Guid.NewGuid());

        Assert.Equal(100.13m, account.SaldoInicial);
        Assert.Equal(100.13m, account.SaldoDisponible);
        Assert.True(account.Estado);
    }

    [Fact]
    public void Create_WithNegativeInitialBalance_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Cuenta.Create("100002", TipoCuenta.Corriente, -0.01m, true, Guid.NewGuid()));

        Assert.Equal("El saldo inicial no puede ser negativo.", exception.Message);
    }


    [Fact]
    public void Create_WithAccountNumberShorterThanSixDigits_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Cuenta.Create("12345", TipoCuenta.Ahorros, 100m, true, Guid.NewGuid()));

        Assert.Equal("El número de cuenta debe contener exactamente 6 dígitos.", exception.Message);
    }

    [Fact]
    public void Create_WithAccountNumberLongerThanSixDigits_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Cuenta.Create("1234567", TipoCuenta.Ahorros, 100m, true, Guid.NewGuid()));

        Assert.Equal("El número de cuenta debe contener exactamente 6 dígitos.", exception.Message);
    }

    [Fact]
    public void Create_WithNonNumericAccountNumber_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Cuenta.Create("12A456", TipoCuenta.Ahorros, 100m, true, Guid.NewGuid()));

        Assert.Equal("El número de cuenta debe contener exactamente 6 dígitos.", exception.Message);
    }

    [Fact]
    public void ApplyMovement_Deposit_IncreasesBalance()
    {
        var account = Account(500m);

        var balance = account.ApplyMovement(125.55m);

        Assert.Equal(625.55m, balance);
        Assert.Equal(625.55m, account.SaldoDisponible);
    }

    [Fact]
    public void ApplyMovement_Withdrawal_DecreasesBalance()
    {
        var account = Account(500m);

        var balance = account.ApplyMovement(-125.55m);

        Assert.Equal(374.45m, balance);
        Assert.Equal(374.45m, account.SaldoDisponible);
    }

    [Fact]
    public void ApplyMovement_Overdraw_ThrowsExactBusinessExceptionAndKeepsBalance()
    {
        var account = Account(100m);

        var exception = Assert.Throws<InsufficientBalanceException>(() => account.ApplyMovement(-100.01m));

        Assert.Equal("Saldo no disponible", exception.Message);
        Assert.Equal(100m, account.SaldoDisponible);
    }

    [Fact]
    public void ApplyMovement_InactiveAccount_RejectsMovement()
    {
        var account = Cuenta.Create("100003", TipoCuenta.Ahorros, 100m, false, Guid.NewGuid());

        var exception = Assert.Throws<DomainException>(() => account.ApplyMovement(10m));

        Assert.Equal("La cuenta se encuentra inactiva.", exception.Message);
        Assert.Equal(100m, account.SaldoDisponible);
    }

    [Fact]
    public void ApplyMovement_ZeroValue_RejectsMovement()
    {
        var account = Account(100m);

        var exception = Assert.Throws<DomainException>(() => account.ApplyMovement(0m));

        Assert.Equal("El valor del movimiento no puede ser cero.", exception.Message);
        Assert.Equal(100m, account.SaldoDisponible);
    }

    [Fact]
    public void RecalculateBalance_OrdersMovementsChronologicallyAndRewritesRunningBalances()
    {
        var account = Account(1000m);
        var first = Movimiento.Create(account.Id, 200m, 0m, new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        var second = Movimiento.Create(account.Id, -300m, 0m, new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc));

        account.RecalculateBalance(new[] { second, first });

        Assert.Equal(1200m, first.Saldo);
        Assert.Equal(900m, second.Saldo);
        Assert.Equal(900m, account.SaldoDisponible);
    }

    private static int _accountSequence = 700000;

    private static Cuenta Account(decimal initialBalance)
        => Cuenta.Create(Interlocked.Increment(ref _accountSequence).ToString(), TipoCuenta.Ahorros, initialBalance, true, Guid.NewGuid());
}
