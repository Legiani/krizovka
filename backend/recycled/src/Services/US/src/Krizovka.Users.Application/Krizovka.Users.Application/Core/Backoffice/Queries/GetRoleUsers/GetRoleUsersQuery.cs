using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Queries.GetRoleUsers;

public record GetRoleUsersQuery(Guid RoleId) : IRequest<GetRoleUsersResponse>;

public record GetRoleUsersResponse(List<RoleUserDto> Users);

public record RoleUserDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    DateTimeOffset AssignedAt
);

public class GetRoleUsersQueryHandler : IRequestHandler<GetRoleUsersQuery, GetRoleUsersResponse>
{
    private readonly IBackofficeRoleRepository _roleRepository;

    public GetRoleUsersQueryHandler(IBackofficeRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<GetRoleUsersResponse> Handle(GetRoleUsersQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
            return new GetRoleUsersResponse([]);

        var users = role.Users.Select(u => new RoleUserDto(
            u.Id,
            u.Email,
            u.FirstName ?? "",
            u.LastName ?? "",
            DateTimeOffset.MinValue  // BackofficeUser nemá CreatedAt – placeholder
        )).ToList();

        return new GetRoleUsersResponse(users);
    }
}
