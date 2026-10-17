using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.UpdateBackofficeUser;

public class UpdateBackofficeUserHandler : IRequestHandler<UpdateBackofficeUserCommand>
{
    private readonly IBackofficeUserRepository _repository;

    public UpdateBackofficeUserHandler(IBackofficeUserRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateBackofficeUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"Backoffice User with ID {request.Id} not found.");
        }

        user.Email = request.WorkingEmail.Trim().ToLowerInvariant();
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.RoleId = request.RoleId;
        user.IsSuperAdmin = request.IsSuperAdmin;

        await _repository.UpdateAsync(user, cancellationToken);
    }
}

