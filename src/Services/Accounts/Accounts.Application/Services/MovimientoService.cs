using Accounts.Application.Abstractions;
using Accounts.Application.DTOs;
using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;

namespace Accounts.Application.Services;

public sealed class MovimientoService
{
    private readonly ICuentaRepository _cuentas;
    private readonly IMovimientoRepository _movimientos;
    private readonly IAccountsUnitOfWork _uow;
    private readonly ITransactionRunner _transactionRunner;

    public MovimientoService(
        ICuentaRepository cuentas,
        IMovimientoRepository movimientos,
        IAccountsUnitOfWork uow,
        ITransactionRunner transactionRunner)
    {
        _cuentas = cuentas;
        _movimientos = movimientos;
        _uow = uow;
        _transactionRunner = transactionRunner;
    }

    public async Task<IReadOnlyCollection<MovimientoResponse>> GetAllAsync(
        string? numeroCuenta,
        DateTime? desde,
        DateTime? hasta,
        CancellationToken ct = default)
    {
        var list = await _movimientos.GetAllAsync(numeroCuenta, Normalize(desde), Normalize(hasta), ct);
        var accountNumbers = new Dictionary<Guid, string>();
        var responses = new List<MovimientoResponse>(list.Count);

        foreach (var movement in list)
        {
            if (!accountNumbers.TryGetValue(movement.CuentaId, out var number))
            {
                var account = await _cuentas.GetByIdAsync(movement.CuentaId, ct);
                number = account?.NumeroCuenta ?? "N/D";
                accountNumbers[movement.CuentaId] = number;
            }
            responses.Add(Map(movement, number));
        }
        return responses;
    }

    public async Task<MovimientoResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var movement = await _movimientos.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Movimiento no encontrado.");
        var account = await _cuentas.GetByIdAsync(movement.CuentaId, ct) ?? throw new KeyNotFoundException("Cuenta no encontrada.");
        return Map(movement, account.NumeroCuenta);
    }

    public Task<MovimientoResponse> CreateAsync(CreateMovimientoRequest request, CancellationToken ct = default)
        => _transactionRunner.ExecuteSerializableAsync(async txCt =>
        {
            var cuenta = await _cuentas.GetByNumeroAsync(request.NumeroCuenta, txCt)
                ?? throw new KeyNotFoundException("Cuenta no encontrada.");

            var saldo = cuenta.ApplyMovement(request.Valor);
            var movimiento = Movimiento.Create(cuenta.Id, request.Valor, saldo, request.Fecha);
            await _movimientos.AddAsync(movimiento, txCt);
            await _uow.SaveChangesAsync(txCt);
            return Map(movimiento, cuenta.NumeroCuenta);
        }, ct);

    public Task<MovimientoResponse> UpdateAsync(Guid id, UpdateMovimientoRequest request, CancellationToken ct = default)
        => _transactionRunner.ExecuteSerializableAsync(async txCt =>
        {
            var movimiento = await _movimientos.GetByIdAsync(id, txCt)
                ?? throw new KeyNotFoundException("Movimiento no encontrado.");
            var cuenta = await _cuentas.GetByIdAsync(movimiento.CuentaId, txCt)
                ?? throw new KeyNotFoundException("Cuenta no encontrada.");

            movimiento.Update(request.Valor, request.Fecha);
            var movements = await _movimientos.GetByCuentaIdAsync(cuenta.Id, tracking: true, txCt);
            cuenta.RecalculateBalance(movements);
            await _uow.SaveChangesAsync(txCt);
            return Map(movimiento, cuenta.NumeroCuenta);
        }, ct);

    private static MovimientoResponse Map(Movimiento m, string numeroCuenta) => new(
        m.Id, m.CuentaId, numeroCuenta, m.Fecha,
        m.TipoMovimiento.ToString(), m.Valor, m.Saldo);

    private static DateTime? Normalize(DateTime? value)
        => value.HasValue ? Normalize(value.Value) : null;

    private static DateTime Normalize(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
