namespace Krizovka.Shared.Authentication;

public interface ISystemOwnerScopeResolver
{
    Task<ResolvedOwnerScope?> ResolveSystemScopeAsync(
        string systemCode,
        CancellationToken cancellationToken);
}
