using Krizovka.Users.Application.Core.Interfaces.Services;
using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Infrastructure.Services;

public class ClaimPolicyService : IClaimPolicyService
{
    public AccountType DetermineNewAccountType(AccountType currentType)
    {
        return currentType switch
        {
            AccountType.PARTNER => AccountType.MAIN,
            AccountType.SUB => AccountType.MAIN,
            AccountType.CHILD => AccountType.MAIN,
            _ => currentType
        };
    }

    public bool ShouldRevokeManagesRelationship(AccountType currentType)
    {
        return currentType switch
        {
            AccountType.PARTNER => true,
            AccountType.SUB => true,
            _ => false
        };
    }
}
