namespace Krizovka.Shared.ErrorHandling.Exceptions;

public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}
