using Krizovka.Shared.ErrorHandling.Exceptions;
using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.DeleteBackofficeUser;

public class DeleteBackofficeUserHandler : IRequestHandler<DeleteBackofficeUserCommand>
{
    private readonly IBackofficeUserRepository _repository;

    public DeleteBackofficeUserHandler(IBackofficeUserRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteBackofficeUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Backoffice user {request.Id} not found.");

        // Cannot delete the last super admin
        if (user.IsSuperAdmin)
        {
            var allAdmins = await _repository.GetAllAsync(cancellationToken);
            var superAdminCount = allAdmins.Count(u => u.IsSuperAdmin);
            if (superAdminCount <= 1)
                throw new DomainException("Nelze smazat posledního super administrátora.");
        }

        await _repository.DeleteAsync(request.Id, cancellationToken);
    }
}
