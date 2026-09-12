namespace Accounts.Domain.Entities;

public sealed class ClienteSnapshot
{
    private ClienteSnapshot() { }

    public ClienteSnapshot(Guid clienteId, string nombre, bool estado, DateTime updatedAtUtc)
    {
        ClienteId = clienteId;
        Nombre = nombre.Trim();
        Estado = estado;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid ClienteId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public bool Estado { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Apply(string nombre, bool estado, DateTime updatedAtUtc)
    {
        // Evita que una redelivery/evento fuera de orden sobrescriba una proyección más reciente.
        if (updatedAtUtc < UpdatedAtUtc)
            return;

        Nombre = nombre.Trim();
        Estado = estado;
        UpdatedAtUtc = updatedAtUtc;
    }
}
