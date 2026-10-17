using Krizovka.Users.Application.Core.Context.DTOs;
using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Constants;
using Krizovka.Users.Domain.Enums;
using MediatR;

namespace Krizovka.Users.Application.Core.Context.Queries.ValidateContext;

public class ValidateContextQueryHandler : IRequestHandler<ValidateContextQuery, ContextValidationResult>
{
    private readonly IUserAccountRepository _accountRepository;
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IBackofficeUserRepository _backofficeUserRepository;

    public ValidateContextQueryHandler(
        IUserAccountRepository accountRepository,
        IRoleAssignmentRepository roleAssignmentRepository,
        IBackofficeUserRepository backofficeUserRepository)
    {
        _accountRepository = accountRepository;
        _roleAssignmentRepository = roleAssignmentRepository;
        _backofficeUserRepository = backofficeUserRepository;
    }

    public async Task<ContextValidationResult> Handle(ValidateContextQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account == null || account.Status != AccountStatus.ACTIVE)
        {
            return new ContextValidationResult
            {
                Allowed = false,
                AccountId = request.AccountId,
                Operation = request.Operation
            };
        }

        var assignments = await _roleAssignmentRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var activeRoles = assignments
            .Where(a => a.Status == AccountStatus.ACTIVE
                && a.ValidFrom <= now
                && (a.ValidTo == null || a.ValidTo > now))
            .ToList();

        // Check if user is a backoffice superadmin via IsSuperAdmin field or BO_SUPER_ADMIN role
        var boUser = account.IdentityId.HasValue
            ? await _backofficeUserRepository.GetByIdentityIdAsync(account.IdentityId.Value, cancellationToken)
            : null;
        var isSuperAdmin = boUser?.IsSuperAdmin == true
            || boUser?.Role?.Code == RoleCodes.BO_SUPER_ADMIN
            || activeRoles.Any(a => a.RoleCode == RoleCodes.BO_SUPER_ADMIN);

        if (isSuperAdmin)
        {
            return new ContextValidationResult
            {
                Allowed = true,
                IsValid = true,
                AccountId = request.AccountId,
                IdentityId = account.IdentityId ?? Guid.Empty,
                Operation = request.Operation,
                Roles = activeRoles.Select(a => a.RoleCode).ToList(),
                Claims = new List<string>(),
                Scope = new ScopeInfo { ScopeType = "GLOBAL", ScopeId = "*" }
            };
        }

        // Validate operation matches a role code (or acts as a wildcard if empty/*)
        var allowed = activeRoles.Any() && (
            string.IsNullOrEmpty(request.Operation) ||
            request.Operation == "*" ||
            activeRoles.Any(a => string.Equals(a.RoleCode, request.Operation, StringComparison.OrdinalIgnoreCase))
        );

        var primaryRole = activeRoles.FirstOrDefault();

        return new ContextValidationResult
        {
            Allowed = allowed,
            IsValid = allowed,
            AccountId = request.AccountId,
            IdentityId = account.IdentityId ?? Guid.Empty,
            Operation = request.Operation,
            Roles = activeRoles.Select(a => a.RoleCode).ToList(),
            Claims = new List<string>(),
            Scope = primaryRole != null ? new ScopeInfo
            {
                ScopeType = primaryRole.ScopeType.ToString(),
                ScopeId = primaryRole.ScopeId
            } : null
        };
    }
}
