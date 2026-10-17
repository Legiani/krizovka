using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Application.Core.Interfaces.Services;

public interface IClaimPolicyService
{
    AccountType DetermineNewAccountType(AccountType currentType);
    bool ShouldRevokeManagesRelationship(AccountType currentType);
}
