namespace Clients.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
}
