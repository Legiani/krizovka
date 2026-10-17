using System.Net.Http.Json;
using Krizovka.Shared.ErrorHandling.Exceptions;
using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.UpdateBackofficeUser;

public class UpdateBackofficeUserHandler : IRequestHandler<UpdateBackofficeUserCommand>
{
    private readonly IBackofficeUserRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;

    public UpdateBackofficeUserHandler(IBackofficeUserRepository repository, IHttpClientFactory httpClientFactory)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
    }

    public async Task Handle(UpdateBackofficeUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"Backoffice User with ID {request.Id} not found.");
        }

        // 1. Update denormalized fields in DB
        user.Email = request.WorkingEmail;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.RoleId = request.RoleId;
        user.IsSuperAdmin = request.IsSuperAdmin;

        await _repository.UpdateAsync(user, cancellationToken);

        // 2. Sync with Kratos Traits (Customer inheritance)
        var client = _httpClientFactory.CreateClient("KratosAdmin");
        var updateBody = new[]
        {
            new { op = "replace", path = "/traits/name/first", value = request.FirstName },
            new { op = "replace", path = "/traits/name/last", value = request.LastName }
        };

        var response = await client.PatchAsJsonAsync($"/admin/identities/{user.IdentityId}", updateBody, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            // We log error but don't fail the whole request if DB update succeeded?
            // Better to fail to maintain consistency if 'inherit from customer' is a hard requirement.
            throw new DomainException($"Failed to sync names with Kratos: {response.StatusCode} - {error}");
        }
    }
}
