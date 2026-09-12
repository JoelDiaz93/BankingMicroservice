using Clients.Domain.Exceptions;

namespace Clients.Domain.Entities;

public sealed class Cliente : Persona
{
    private Cliente() { }

    private Cliente(
        Guid id,
        string nombre,
        string genero,
        int edad,
        string identificacion,
        string direccion,
        string telefono,
        string passwordHash,
        bool estado)
        : base(id, nombre, genero, edad, identificacion, direccion, telefono)
    {
        SetPasswordHash(passwordHash);
        Estado = estado;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public string PasswordHash { get; private set; } = string.Empty;
    public bool Estado { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Cliente Create(
        string nombre,
        string genero,
        int edad,
        string identificacion,
        string direccion,
        string telefono,
        string passwordHash,
        bool estado = true)
        => new(
            Guid.NewGuid(), nombre, genero, edad, identificacion,
            direccion, telefono, passwordHash, estado);

    public void Update(
        string nombre,
        string genero,
        int edad,
        string identificacion,
        string direccion,
        string telefono,
        string? passwordHash,
        bool estado)
    {
        SetPersonaData(nombre, genero, edad, identificacion, direccion, telefono);
        if (!string.IsNullOrWhiteSpace(passwordHash))
            SetPasswordHash(passwordHash);

        Estado = estado;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetEstado(bool estado)
    {
        Estado = estado;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("La contraseña es obligatoria.");
        PasswordHash = passwordHash;
    }
}
