using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.CreateRole;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, CreateRoleResponse>
{
    private readonly IBackofficeRoleRepository _roleRepository;

    public CreateRoleCommandHandler(IBackofficeRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<CreateRoleResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = new BackofficeRole
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            Permissions = new List<BackofficePermission>(),
            Users = new List<BackofficeUser>()
        };

        await _roleRepository.CreateAsync(role, cancellationToken);

        return new CreateRoleResponse(role.Id, true);
    }
}
