using Clients.Application.DTOs;
using Clients.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Clients.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public sealed class ClientesController : ControllerBase
{
    private readonly ClienteService _service;
    public ClientesController(ClienteService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ClienteResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ClienteResponse>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteResponse>> GetById(Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ClienteResponse>> Create(CreateClienteRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.ClienteId }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClienteResponse>> Update(Guid id, UpdateClienteRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClienteResponse>> PatchEstado(Guid id, PatchClienteEstadoRequest request, CancellationToken ct)
        => Ok(await _service.PatchEstadoAsync(id, request.Estado, ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
