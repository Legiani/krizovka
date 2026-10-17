using Krizovka.Shared.ErrorHandling.Exceptions;
using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;
using BCrypt.Net;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.ResetBackofficeUserPassword;

public class ResetBackofficeUserPasswordHandler : IRequestHandler<ResetBackofficeUserPasswordCommand>
{
    private readonly IBackofficeUserRepository _repository;

    public ResetBackofficeUserPasswordHandler(IBackofficeUserRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(ResetBackofficeUserPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new ValidationException("Password", "Heslo musí mít alespoň 8 znaků.");

        var user = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Backoffice user {request.Id} not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _repository.UpdateAsync(user, cancellationToken);
    }
}
