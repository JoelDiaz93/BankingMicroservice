namespace Accounts.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public sealed class InsufficientBalanceException : DomainException
{
    public InsufficientBalanceException() : base("Saldo no disponible") { }
}
