using System.ComponentModel.DataAnnotations;

namespace Accounts.Application.DTOs;

public sealed record CreateCuentaRequest(
    [Required, RegularExpression(@"^[0-9]{6}$", ErrorMessage = "El número de cuenta debe contener exactamente 6 dígitos.")] string NumeroCuenta,
    [Required, RegularExpression(@"^(?i:Ahorros|Corriente)$", ErrorMessage = "Tipo de cuenta inválido. Use 'Ahorros' o 'Corriente'.")] string TipoCuenta,
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "El saldo inicial no puede ser negativo.")] decimal SaldoInicial,
    bool Estado,
    Guid ClienteId);

public sealed record UpdateCuentaRequest(
    [Required, RegularExpression(@"^[0-9]{6}$", ErrorMessage = "El número de cuenta debe contener exactamente 6 dígitos.")] string NumeroCuenta,
    [Required, RegularExpression(@"^(?i:Ahorros|Corriente)$", ErrorMessage = "Tipo de cuenta inválido. Use 'Ahorros' o 'Corriente'.")] string TipoCuenta,
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "El saldo inicial no puede ser negativo.")] decimal SaldoInicial,
    bool Estado);

public sealed record PatchCuentaEstadoRequest(bool Estado);

public sealed record CuentaResponse(
    Guid CuentaId,
    string NumeroCuenta,
    string TipoCuenta,
    decimal SaldoInicial,
    decimal SaldoDisponible,
    bool Estado,
    Guid ClienteId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateMovimientoRequest(
    [Required, RegularExpression(@"^[0-9]{6}$", ErrorMessage = "El número de cuenta debe contener exactamente 6 dígitos.")] string NumeroCuenta,
    decimal Valor,
    DateTime? Fecha = null);

public sealed record UpdateMovimientoRequest(decimal Valor, DateTime Fecha);

public sealed record MovimientoResponse(
    Guid MovimientoId,
    Guid CuentaId,
    string NumeroCuenta,
    DateTime Fecha,
    string TipoMovimiento,
    decimal Valor,
    decimal Saldo);

public sealed record ReporteEstadoCuentaResponse(
    Guid ClienteId,
    string Cliente,
    DateTime FechaInicio,
    DateTime FechaFin,
    IReadOnlyCollection<CuentaReporteResponse> Cuentas);

public sealed record CuentaReporteResponse(
    Guid CuentaId,
    string NumeroCuenta,
    string Tipo,
    decimal SaldoInicial,
    decimal SaldoDisponible,
    bool Estado,
    IReadOnlyCollection<MovimientoReporteResponse> Movimientos);

public sealed record MovimientoReporteResponse(
    DateTime Fecha,
    string TipoMovimiento,
    decimal Valor,
    decimal SaldoDisponible);
