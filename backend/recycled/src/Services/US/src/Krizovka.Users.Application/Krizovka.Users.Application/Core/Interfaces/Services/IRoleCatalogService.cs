using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Application.Core.Interfaces.Services;

public interface IRoleCatalogService
{
    List<RoleDefinition> GetAllRoles();
    RoleDefinition? GetRole(string roleCode);
}

public record RoleDefinition(
    string RoleCode,
    string Category,
    ScopeType[] AllowedScopes,
    string? Description = null
);
