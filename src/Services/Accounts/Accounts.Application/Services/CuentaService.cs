using Accounts.Application.Abstractions;
using Accounts.Application.DTOs;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Exceptions;
using Accounts.Domain.Repositories;

namespace Accounts.Application.Services;

public sealed class CuentaService
{
    private readonly ICuentaRepository _cuentas;
    private readonly IMovimientoRepository _movimientos;
    private readonly IClienteSnapshotRepository _clientes;
    private readonly IAccountsUnitOfWork _uow;
    private readonly ITransactionRunner _transactionRunner;

    public CuentaService(
        ICuentaRepository cuentas,
        IMovimientoRepository movimientos,
        IClienteSnapshotRepository clientes,
        IAccountsUnitOfWork uow,
        ITransactionRunner transactionRunner)
    {
        _cuentas = cuentas;
        _movimientos = movimientos;
        _clientes = clientes;
        _uow = uow;
        _transactionRunner = transactionRunner;
    }

    public async Task<IReadOnlyCollection<CuentaResponse>> GetAllAsync(CancellationToken ct = default)
        => (await _cuentas.GetAllAsync(ct)).Select(Map).ToArray();

    public async Task<CuentaResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cuenta = await _cuentas.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Cuenta no encontrada.");
        return Map(cuenta);
    }

    public async Task<CuentaResponse> CreateAsync(CreateCuentaRequest request, CancellationToken ct = default)
    {
        var cliente = await _clientes.GetByIdAsync(request.ClienteId, ct)
            ?? throw new DomainException("El cliente indicado no existe en la proyección local.");
        if (!cliente.Estado)
            throw new DomainException("No se puede crear una cuenta para un cliente inactivo.");
        if (await _cuentas.ExistsByNumeroAsync(request.NumeroCuenta, null, ct))
            throw new DomainException("El número de cuenta ya existe.");

        var cuenta = Cuenta.Create(
            request.NumeroCuenta,
            ParseTipoCuenta(request.TipoCuenta),
            request.SaldoInicial,
            request.Estado,
            request.ClienteId);

        await _cuentas.AddAsync(cuenta, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(cuenta);
    }

    public Task<CuentaResponse> UpdateAsync(Guid id, UpdateCuentaRequest request, CancellationToken ct = default)
        => _transactionRunner.ExecuteSerializableAsync(async txCt =>
        {
            var cuenta = await _cuentas.GetByIdAsync(id, txCt) ?? throw new KeyNotFoundException("Cuenta no encontrada.");
            if (await _cuentas.ExistsByNumeroAsync(request.NumeroCuenta, id, txCt))
                throw new DomainException("El número de cuenta ya existe.");

            cuenta.Update(request.NumeroCuenta, ParseTipoCuenta(request.TipoCuenta), request.SaldoInicial, request.Estado);
            var movimientos = await _movimientos.GetByCuentaIdAsync(cuenta.Id, tracking: true, txCt);
            cuenta.RecalculateBalance(movimientos);
            await _uow.SaveChangesAsync(txCt);
            return Map(cuenta);
        }, ct);

    public async Task<CuentaResponse> PatchEstadoAsync(Guid id, bool estado, CancellationToken ct = default)
    {
        var cuenta = await _cuentas.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Cuenta no encontrada.");
        cuenta.SetEstado(estado);
        await _uow.SaveChangesAsync(ct);
        return Map(cuenta);
    }

    internal static TipoCuenta ParseTipoCuenta(string value)
    {
        if (!Enum.TryParse<TipoCuenta>(value, ignoreCase: true, out var result))
            throw new DomainException("Tipo de cuenta inválido. Use 'Ahorros' o 'Corriente'.");
        return result;
    }

    internal static CuentaResponse Map(Cuenta c) => new(
        c.Id, c.NumeroCuenta, c.TipoCuenta.ToString(), c.SaldoInicial,
        c.SaldoDisponible, c.Estado, c.ClienteId, c.CreatedAtUtc, c.UpdatedAtUtc);
}
