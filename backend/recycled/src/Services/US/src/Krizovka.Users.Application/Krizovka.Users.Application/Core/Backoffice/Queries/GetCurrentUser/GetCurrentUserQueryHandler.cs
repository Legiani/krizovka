using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;
using Krizovka.Users.Domain.Constants;
using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Application.Core.Backoffice.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, GetCurrentUserResponse?>
{
    private readonly IBackofficeUserRepository _backofficeUserRepository;
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;

    public GetCurrentUserQueryHandler(
        IBackofficeUserRepository backofficeUserRepository,
        IRoleAssignmentRepository roleAssignmentRepository)
    {
        _backofficeUserRepository = backofficeUserRepository;
        _roleAssignmentRepository = roleAssignmentRepository;
    }

    public async Task<GetCurrentUserResponse?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        // IMPORTANT: The request.IdentityId contains either BackofficeUser.IdentityId (from Kratos) or BackofficeUser.Id (old cookies).
        var boUser = await _backofficeUserRepository.GetByIdentityIdAsync(request.IdentityId, cancellationToken)
            ?? await _backofficeUserRepository.GetByIdAsync(request.IdentityId, cancellationToken);

        if (boUser == null || !boUser.IsActive)
        {
            return null;
        }

        var isSuperAdmin = boUser.IsSuperAdmin || boUser.Role?.Code == RoleCodes.BO_SUPER_ADMIN;

        var roleInfo = boUser.Role != null ? new RoleInfo(
            boUser.Role.Id,
            boUser.Role.Name,
            boUser.Role.Code,
            boUser.Role.Description
        ) : null;

        var permissions = new List<PermissionInfo>();
        if (isSuperAdmin)
        {
            permissions.Add(new PermissionInfo("*", "*", "GLOBAL", "*"));
        }
        else if (boUser.Role != null && boUser.Role.Permissions != null)
        {
            permissions = boUser.Role.Permissions.Select(p => new PermissionInfo(
                p.Module,
                p.Action,
                p.ScopeType,
                p.ScopeValue
            )).ToList();
        }

        // Load city assignments from RoleAssignment table
        // Super-admins get empty list (they have global access via isSuperAdmin flag)
        var cityAssignments = new List<CityAssignmentInfo>();
        if (!isSuperAdmin)
        {
            var allAssignments = await _roleAssignmentRepository.GetByAccountIdAsync(boUser.Id, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            cityAssignments = allAssignments
                .Where(a => a.Status == AccountStatus.ACTIVE
                    && a.ScopeType == ScopeType.CITY
                    && !string.IsNullOrEmpty(a.ScopeId)
                    && a.ValidFrom <= now
                    && (a.ValidTo == null || a.ValidTo > now))
                .Select(a => new CityAssignmentInfo(a.ScopeId, a.RoleCode))
                .DistinctBy(a => a.CityId)
                .ToList();
        }

        return new GetCurrentUserResponse(
            boUser.Id, // UserId
            boUser.IdentityId ?? boUser.Id, // IdentityId
            boUser.Id, // AccountId
            boUser.Email,
            boUser.FirstName ?? string.Empty,
            boUser.LastName ?? string.Empty,
            isSuperAdmin,
            roleInfo,
            permissions,
            cityAssignments
        );
    }
}
