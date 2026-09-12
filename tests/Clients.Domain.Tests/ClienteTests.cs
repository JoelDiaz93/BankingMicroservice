using Clients.Domain.Entities;
using Clients.Domain.Exceptions;
using Xunit;

namespace Clients.Domain.Tests;

public sealed class ClienteTests
{
    [Fact]
    public void Create_WithValidData_CreatesActiveClient()
    {
        var client = Cliente.Create(
            "Jose Lema", "Masculino", 35, "1100000001",
            "Otavalo sn y principal", "098254785", "hash-seguro", true);

        Assert.NotEqual(Guid.Empty, client.Id);
        Assert.Equal("Jose Lema", client.Nombre);
        Assert.True(client.Estado);
        Assert.Equal("1100000001", client.Identificacion);
    }

    [Fact]
    public void Create_WithInvalidAge_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Cliente.Create(
            "Jose Lema", "Masculino", -1, "1100000001",
            "Otavalo sn y principal", "098254785", "hash-seguro", true));
    }


    [Fact]
    public void Create_WithIdentificationShorterThanTenDigits_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => Cliente.Create(
            "Jose Lema", "Masculino", 35, "110000001",
            "Otavalo sn y principal", "098254785", "hash-seguro", true));

        Assert.Equal("La identificación debe contener exactamente 10 dígitos.", exception.Message);
    }

    [Fact]
    public void Create_WithIdentificationLongerThanTenDigits_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => Cliente.Create(
            "Jose Lema", "Masculino", 35, "11000000011",
            "Otavalo sn y principal", "098254785", "hash-seguro", true));

        Assert.Equal("La identificación debe contener exactamente 10 dígitos.", exception.Message);
    }

    [Fact]
    public void Create_WithNonNumericIdentification_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => Cliente.Create(
            "Jose Lema", "Masculino", 35, "11000000A1",
            "Otavalo sn y principal", "098254785", "hash-seguro", true));

        Assert.Equal("La identificación debe contener exactamente 10 dígitos.", exception.Message);
    }

    [Fact]
    public void SetEstado_ChangesClientStatus()
    {
        var client = Cliente.Create(
            "Jose Lema", "Masculino", 35, "1100000001",
            "Otavalo sn y principal", "098254785", "hash-seguro", true);

        client.SetEstado(false);

        Assert.False(client.Estado);
    }
}
