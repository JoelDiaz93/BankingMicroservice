using Clients.Domain.Exceptions;

namespace Clients.Domain.Entities;

public abstract class Persona
{
    protected Persona() { }

    protected Persona(
        Guid id,
        string nombre,
        string genero,
        int edad,
        string identificacion,
        string direccion,
        string telefono)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetPersonaData(nombre, genero, edad, identificacion, direccion, telefono);
    }

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Genero { get; private set; } = string.Empty;
    public int Edad { get; private set; }
    public string Identificacion { get; private set; } = string.Empty;
    public string Direccion { get; private set; } = string.Empty;
    public string Telefono { get; private set; } = string.Empty;

    protected void SetPersonaData(
        string nombre,
        string genero,
        int edad,
        string identificacion,
        string direccion,
        string telefono)
    {
        var normalizedNombre = Required(nombre, "El nombre es obligatorio.");
        if (normalizedNombre.Length > 150)
            throw new DomainException("El nombre no puede superar 150 caracteres.");

        var normalizedGenero = Required(genero, "El género es obligatorio.");
        if (normalizedGenero.Length > 30)
            throw new DomainException("El género no puede superar 30 caracteres.");

        if (edad is < 0 or > 130)
            throw new DomainException("La edad debe estar entre 0 y 130.");

        var normalizedIdentification = Required(identificacion, "La identificación es obligatoria.");
        if (normalizedIdentification.Length != 10 || !normalizedIdentification.All(IsAsciiDigit))
            throw new DomainException("La identificación debe contener exactamente 10 dígitos.");

        var normalizedDireccion = Required(direccion, "La dirección es obligatoria.");
        if (normalizedDireccion.Length > 250)
            throw new DomainException("La dirección no puede superar 250 caracteres.");

        var normalizedTelefono = Required(telefono, "El teléfono es obligatorio.");
        if (normalizedTelefono.Length > 30)
            throw new DomainException("El teléfono no puede superar 30 caracteres.");

        Nombre = normalizedNombre;
        Genero = normalizedGenero;
        Edad = edad;
        Identificacion = normalizedIdentification;
        Direccion = normalizedDireccion;
        Telefono = normalizedTelefono;
    }

    private static string Required(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(message);
        return value.Trim();
    }

    private static bool IsAsciiDigit(char value) => value is >= '0' and <= '9';
}
