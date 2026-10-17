using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Queries.GetBackofficeUser;

public class GetBackofficeUserHandler : IRequestHandler<GetBackofficeUserQuery, BackofficeUserDetailDto?>
{
    private readonly IBackofficeUserRepository _repository;

    public GetBackofficeUserHandler(IBackofficeUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<BackofficeUserDetailDto?> Handle(GetBackofficeUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null) return null;

        var permissions = new List<string>();
        // Add Role permissions
        if (user.Role?.Permissions != null)
        {
            permissions.AddRange(user.Role.Permissions.Select(p => $"{p.Module}:{p.Action}"));
        }

        // Add Direct permissions (if any, though BackofficeUser doesn't have direct permissions per previous checks, only Role)
        // I checked BackofficeUser, it didn't have Permissions prop.
        // So just Role permissions.

        return new BackofficeUserDetailDto(
            user.Id,
            user.IdentityId,
            user.AccountId,
            user.Email,
            user.FirstName,
            user.LastName,
            user.IsSuperAdmin,
            user.RoleId,
            user.Role?.Name,
            user.Role?.Permissions.Select(p => p.Module + ":" + p.Action).ToList() ?? new List<string>()
        );
    }
}
