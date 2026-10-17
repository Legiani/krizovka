namespace Krizovka.Users.Domain.Enums;

public enum RelationType
{
    PARENT_OF,
    MANAGES,
    PARTNER_OF,
    GUARDIAN_OF,
    MANAGES_ORGANIZATION_STAFF,
    MANAGES_PROVIDER_STAFF  // IDS Backoffice user manages staff of a PROVIDER/CARRIER
}
