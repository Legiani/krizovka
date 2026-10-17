using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Queries.ListUsers;

public class ListBackofficeUsersHandler : IRequestHandler<ListBackofficeUsersQuery, List<BackofficeUserDto>>
{
    private readonly IBackofficeUserRepository _repository;

    public ListBackofficeUsersHandler(IBackofficeUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<BackofficeUserDto>> Handle(ListBackofficeUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _repository.GetAllAsync(cancellationToken);

        return users.Select(u => new BackofficeUserDto(
            u.Id,
            u.IdentityId,
            u.AccountId,
            u.Email,
            u.FirstName,
            u.LastName,
            u.IsSuperAdmin,
            u.Role?.Name,
            u.Role?.Code
        )).ToList();
    }
}
