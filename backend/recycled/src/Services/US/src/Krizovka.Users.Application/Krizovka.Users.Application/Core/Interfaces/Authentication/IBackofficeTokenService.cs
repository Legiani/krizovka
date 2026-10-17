using Krizovka.Users.Domain.Entities;

namespace Krizovka.Users.Application.Core.Interfaces.Authentication;

public interface IBackofficeTokenService
{
    string GenerateToken(BackofficeUser user);
}
