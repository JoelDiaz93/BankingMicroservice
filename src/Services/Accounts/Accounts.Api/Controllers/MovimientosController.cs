using Accounts.Application.DTOs;
using Accounts.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Accounts.Api.Controllers;

[ApiController]
[Route("api/movimientos")]
public sealed class MovimientosController : ControllerBase
{
    private readonly MovimientoService _service;
    public MovimientosController(MovimientoService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MovimientoResponse>>> GetAll(
        [FromQuery] string? numeroCuenta,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        CancellationToken ct)
        => Ok(await _service.GetAllAsync(numeroCuenta, desde, hasta, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MovimientoResponse>> GetById(Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<MovimientoResponse>> Create(CreateMovimientoRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.MovimientoId }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MovimientoResponse>> Update(Guid id, UpdateMovimientoRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));
}
