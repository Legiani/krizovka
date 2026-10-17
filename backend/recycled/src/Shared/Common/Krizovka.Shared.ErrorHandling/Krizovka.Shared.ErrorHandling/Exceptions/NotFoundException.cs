namespace Krizovka.Shared.ErrorHandling.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with ID '{key}' was not found") { }
}
