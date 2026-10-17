namespace Krizovka.Shared.ErrorHandling.Exceptions;

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message = "Unauthorized access") : base(message) { }
}
