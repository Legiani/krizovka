using Krizovka.Shared.ErrorHandling.Exceptions;

namespace Krizovka.Users.Domain.Exceptions;

public class ActingRequiredException : ForbiddenException
{
    public ActingRequiredException(string message) : base(message) { }
}
