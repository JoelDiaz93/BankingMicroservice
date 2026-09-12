using Accounts.Application.DTOs;
using Accounts.Domain.Repositories;

namespace Accounts.Application.Services;

public sealed class ReporteService
{
    private readonly IClienteSnapshotRepository _clientes;
    private readonly ICuentaRepository _cuentas;
    private readonly IMovimientoRepository _movimientos;

    public ReporteService(
        IClienteSnapshotRepository clientes,
        ICuentaRepository cuentas,
        IMovimientoRepository movimientos)
    {
        _clientes = clientes;
        _cuentas = cuentas;
        _movimientos = movimientos;
    }

    public async Task<ReporteEstadoCuentaResponse> GenerateAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        string cliente,
        CancellationToken ct = default)
    {
        fechaInicio = NormalizeUtc(fechaInicio.Date);
        fechaFin = NormalizeUtc(fechaFin.Date.AddDays(1).AddTicks(-1));
        if (fechaInicio > fechaFin)
            throw new ArgumentException("La fecha inicial debe ser menor o igual a la fecha final.");
        if (string.IsNullOrWhiteSpace(cliente))
            throw new ArgumentException("El filtro cliente es obligatorio.");

        var client = await _clientes.FindByFilterAsync(cliente.Trim(), ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado en la proyección de cuentas.");

        var accounts = await _cuentas.GetByClienteIdAsync(client.ClienteId, tracking: false, ct);
        var ids = accounts.Select(x => x.Id).ToArray();
        var movements = ids.Length == 0
            ? Array.Empty<Accounts.Domain.Entities.Movimiento>()
            : (await _movimientos.GetByCuentaIdsAndRangeAsync(ids, fechaInicio, fechaFin, ct)).ToArray();

        var resultAccounts = accounts.Select(account => new CuentaReporteResponse(
            account.Id,
            account.NumeroCuenta,
            account.TipoCuenta.ToString(),
            account.SaldoInicial,
            account.SaldoDisponible,
            account.Estado,
            movements
                .Where(x => x.CuentaId == account.Id)
                .OrderBy(x => x.Fecha)
                .Select(x => new MovimientoReporteResponse(
                    x.Fecha, x.TipoMovimiento.ToString(), x.Valor, x.Saldo))
                .ToArray()
        )).ToArray();

        return new ReporteEstadoCuentaResponse(
            client.ClienteId,
            client.Nombre,
            fechaInicio.Date,
            fechaFin.Date,
            resultAccounts);
    }

    private static DateTime NormalizeUtc(DateTime value)
        => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
