using Accounts.Application.DTOs;
using Accounts.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Accounts.Api.Controllers;

[ApiController]
[Route("api/cuentas")]
public sealed class CuentasController : ControllerBase
{
    private readonly CuentaService _service;
    public CuentasController(CuentaService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CuentaResponse>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CuentaResponse>> GetById(Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CuentaResponse>> Create(CreateCuentaRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.CuentaId }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CuentaResponse>> Update(Guid id, UpdateCuentaRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [HttpPatch("{id:guid}/estado")]
    public async Task<ActionResult<CuentaResponse>> PatchEstado(Guid id, PatchCuentaEstadoRequest request, CancellationToken ct)
        => Ok(await _service.PatchEstadoAsync(id, request.Estado, ct));
}
