using System.ComponentModel.DataAnnotations;

namespace Clients.Application.DTOs;

public sealed record CreateClienteRequest(
    [Required, StringLength(150)] string Nombre,
    [Required, StringLength(30)] string Genero,
    [Range(0, 130)] int Edad,
    [Required, RegularExpression(@"^[0-9]{10}$", ErrorMessage = "La identificación debe contener exactamente 10 dígitos.")] string Identificacion,
    [Required, StringLength(250)] string Direccion,
    [Required, StringLength(30)] string Telefono,
    [Required, MinLength(4)] string Contrasena,
    bool Estado = true);

public sealed record UpdateClienteRequest(
    [Required, StringLength(150)] string Nombre,
    [Required, StringLength(30)] string Genero,
    [Range(0, 130)] int Edad,
    [Required, RegularExpression(@"^[0-9]{10}$", ErrorMessage = "La identificación debe contener exactamente 10 dígitos.")] string Identificacion,
    [Required, StringLength(250)] string Direccion,
    [Required, StringLength(30)] string Telefono,
    [MinLength(4)] string? Contrasena,
    bool Estado);

public sealed record PatchClienteEstadoRequest(bool Estado);

public sealed record ClienteResponse(
    Guid ClienteId,
    string Nombre,
    string Genero,
    int Edad,
    string Identificacion,
    string Direccion,
    string Telefono,
    bool Estado,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
