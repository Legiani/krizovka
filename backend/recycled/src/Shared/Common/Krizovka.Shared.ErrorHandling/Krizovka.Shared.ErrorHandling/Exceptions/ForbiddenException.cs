namespace Krizovka.Shared.ErrorHandling.Exceptions;

public class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "Access forbidden") : base(message) { }
}
