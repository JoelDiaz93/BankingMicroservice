using System.Globalization;
using Accounts.Application.DTOs;
using Accounts.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Accounts.Api.Controllers;

[ApiController]
[Route("api/reportes")]
public sealed class ReportesController : ControllerBase
{
    private readonly ReporteService _service;
    public ReportesController(ReporteService service) => _service = service;

    /// <summary>
    /// Estado de cuenta por rango de fechas y cliente.
    /// Compatible con: /api/reportes?fecha=2022-02-01,2022-02-28&amp;cliente=Marianela%20Montalvo
    /// También admite fechaInicio y fechaFin por separado.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ReporteEstadoCuentaResponse>> Get(
        [FromQuery] string? fecha,
        [FromQuery] string cliente,
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        CancellationToken ct)
    {
        var (from, to) = ResolveDates(fecha, fechaInicio, fechaFin);
        return Ok(await _service.GenerateAsync(from, to, cliente, ct));
    }

    private static (DateTime From, DateTime To) ResolveDates(string? fecha, DateTime? fechaInicio, DateTime? fechaFin)
    {
        if (fechaInicio.HasValue && fechaFin.HasValue)
            return (fechaInicio.Value, fechaFin.Value);

        if (string.IsNullOrWhiteSpace(fecha))
            throw new ArgumentException("Indique fechaInicio/fechaFin o el parámetro fecha con un rango.");

        var normalized = fecha.Replace("..", "|").Replace(";", "|").Replace(",", "|");
        var parts = normalized.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !TryParseDate(parts[0], out var from) || !TryParseDate(parts[1], out var to))
            throw new ArgumentException("Formato de fecha inválido. Use fecha=2022-02-01,2022-02-28.");
        return (from, to);
    }

    private static bool TryParseDate(string value, out DateTime result)
    {
        var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "M/d/yyyy" };
        return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
            || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }
}
