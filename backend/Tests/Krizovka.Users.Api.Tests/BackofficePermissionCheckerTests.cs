using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Infrastructure.Services;
using Xunit;

namespace Krizovka.Users.Api.Tests;

public class BackofficePermissionCheckerTests
{
    private sealed class FakeBackofficeUserRepository : IBackofficeUserRepository
    {
        private readonly BackofficeUser? _user;

        public FakeBackofficeUserRepository(BackofficeUser? user)
        {
            _user = user;
        }

        public Task<BackofficeUser?> GetByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken = default)
            => Task.FromResult(_user);

        public Task<BackofficeUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_user);

        public Task<BackofficeUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(_user);

        public Task<List<BackofficeUser>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_user != null ? new List<BackofficeUser> { _user } : new List<BackofficeUser>());

        public Task AddAsync(BackofficeUser user, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(BackofficeUser user, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(_user != null);
    }

    [Fact]
    public async Task SuperAdmin_Always_Has_Permission()
    {
        var identityId = Guid.NewGuid();
        var user = new BackofficeUser
        {
            Id = Guid.NewGuid(),
            IdentityId = identityId,
            Email = "superadmin@krizovka.cz",
            IsActive = true,
            IsSuperAdmin = true,
            PasswordHash = "hash"
        };

        var checker = new BackofficePermissionChecker(new FakeBackofficeUserRepository(user));

        var result = await checker.HasPermissionAsync(identityId.ToString(), "USERS", "READ");
        Assert.True(result);
    }

    [Fact]
    public async Task Operator_Without_Permissions_Is_Forbidden()
    {
        var identityId = Guid.NewGuid();
        var role = new BackofficeRole
        {
            Id = Guid.NewGuid(),
            Code = "OPERATOR",
            Name = "Operator",
            Permissions = new List<BackofficePermission>()
        };

        var user = new BackofficeUser
        {
            Id = Guid.NewGuid(),
            IdentityId = identityId,
            Email = "operator@krizovka.cz",
            IsActive = true,
            IsSuperAdmin = false,
            Role = role,
            RoleId = role.Id,
            PasswordHash = "hash"
        };

        var checker = new BackofficePermissionChecker(new FakeBackofficeUserRepository(user));

        var result = await checker.HasPermissionAsync(identityId.ToString(), "USERS", "READ");
        Assert.False(result);
    }

    [Fact]
    public async Task Operator_With_Users_Wildcard_Has_Permission()
    {
        var identityId = Guid.NewGuid();
        var role = new BackofficeRole
        {
            Id = Guid.NewGuid(),
            Code = "OPERATOR",
            Name = "Operator",
            Permissions = new List<BackofficePermission>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Module = "USERS",
                    Action = "*",
                    ScopeType = "GLOBAL",
                    ScopeValue = "*"
                }
            }
        };

        var user = new BackofficeUser
        {
            Id = Guid.NewGuid(),
            IdentityId = identityId,
            Email = "operator@krizovka.cz",
            IsActive = true,
            IsSuperAdmin = false,
            Role = role,
            RoleId = role.Id,
            PasswordHash = "hash"
        };

        var checker = new BackofficePermissionChecker(new FakeBackofficeUserRepository(user));

        var result = await checker.HasPermissionAsync(identityId.ToString(), "USERS", "READ");
        Assert.True(result);
    }
}
