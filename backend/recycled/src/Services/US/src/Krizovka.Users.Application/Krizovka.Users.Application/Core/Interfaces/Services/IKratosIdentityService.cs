using Krizovka.Users.Application.Core.Interfaces.Services.Models;
namespace Krizovka.Users.Application.Core.Interfaces.Services;

public interface IKratosIdentityService
{
    Task<KratosIdentityDto?> GetIdentityAsync(Guid identityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Guid>> FindIdentityIdsByCredentialAsync(string credential, CancellationToken cancellationToken = default);
}
