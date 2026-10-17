using Krizovka.Shared.ErrorHandling.Exceptions;

namespace Krizovka.Users.Domain.Exceptions;

public class ActingNotAllowedException : ForbiddenException
{
    public ActingNotAllowedException(string message) : base(message) { }
}
