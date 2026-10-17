using Krizovka.Users.Application.Core.Interfaces.Authentication;
using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.Login;

public class BackofficeLoginCommandHandler : IRequestHandler<BackofficeLoginCommand, LoginResponse>
{
    private readonly IBackofficeUserRepository _repository;
    private readonly IBackofficeTokenService _tokenService;

    public BackofficeLoginCommandHandler(
        IBackofficeUserRepository repository,
        IBackofficeTokenService tokenService)
    {
        _repository = repository;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> Handle(BackofficeLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null || !user.IsActive)
        {
            return new LoginResponse(false, null, "Nesprávný email nebo heslo, nebo je účet neaktivní.", null, null, null);
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return new LoginResponse(false, null, "Účet nemá nastavené heslo. Kontaktujte správce.", null, null, null);
        }

        bool passwordValid = false;
        try
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        }
        catch (Exception)
        {
            return new LoginResponse(false, null, "Účet obsahuje neplatná kryptografická data. Kontaktujte správce.", null, null, null);
        }

        if (!passwordValid)
        {
            return new LoginResponse(false, null, "Nesprávný email nebo heslo.", null, null, null);
        }

        var token = _tokenService.GenerateToken(user);

        return new LoginResponse(
            true,
            token,
            null,
            user.Id,
            user.IdentityId,
            user.Role?.Code
        );
    }
}
