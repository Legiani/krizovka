using Krizovka.Shared.ErrorHandling.Exceptions;
using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.CreateBackofficeUser;

public class CreateBackofficeUserHandler : IRequestHandler<CreateBackofficeUserCommand, Guid>
{
    private readonly IBackofficeUserRepository _userRepository;

    public CreateBackofficeUserHandler(IBackofficeUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Guid> Handle(CreateBackofficeUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.WorkingEmail, cancellationToken))
        {
            throw new ConflictException($"Backoffice user with email {request.WorkingEmail} already exists.");
        }

        var userId = Guid.NewGuid();
        var user = new BackofficeUser
        {
            Id = userId,
            IdentityId = userId,
            AccountId = request.AccountId,
            Email = request.WorkingEmail.Trim().ToLowerInvariant(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IsSuperAdmin = request.IsSuperAdmin,
            IsActive = true,
            RoleId = request.RoleId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Krizovka123!")
        };

        await _userRepository.AddAsync(user, cancellationToken);
        return user.Id;
    }
}
