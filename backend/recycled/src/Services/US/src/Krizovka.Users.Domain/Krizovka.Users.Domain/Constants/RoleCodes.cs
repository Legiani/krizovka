namespace Krizovka.Users.Domain.Constants;

public static class RoleCodes
{
    // ─── Backoffice Platform Roles ────────────────────────────────────────────
    public const string BO_SUPER_ADMIN = "BO_SUPER_ADMIN";
    public const string BO_ADMIN = "BO_ADMIN";
    public const string BO_USER = "BO_USER";

    // ─── Transport Backoffice Roles (IDS/PROVIDER/CARRIER scoped) ────────────
    /// <summary>Can manage tariffs, zones and price rules for a given IDS/CITY scope.</summary>
    public const string BO_TARIFF_ADMIN = "BO_TARIFF_ADMIN";
    /// <summary>Read-only access to reports and ABT dashboards.</summary>
    public const string BO_REPORT_VIEWER = "BO_REPORT_VIEWER";
    /// <summary>Card approval authority (MOS card approvals for IDS).</summary>
    public const string BO_CARD_APPROVER = "BO_CARD_APPROVER";
    /// <summary>Info-centre agent – customer support, order lookup (IDS scope).</summary>
    public const string INFOCENTER_AGENT = "INFOCENTER_AGENT";
    /// <summary>Carrier/provider staff – limited ops access within CARRIER or IDS scope.</summary>
    public const string CARRIER_STAFF = "CARRIER_STAFF";

    // ─── Benefit Organisation Roles (ORGANIZATION scoped) ────────────────────
    public const string BENEFIT_ORGANIZATION_ADMIN = "BENEFIT_ORGANIZATION_ADMIN";
    public const string BENEFIT_ORGANIZATION_SUPPORT = "BENEFIT_ORGANIZATION_SUPPORT";
    public const string BENEFIT_ORGANIZATION_FINANCE = "BENEFIT_ORGANIZATION_FINANCE";

    // ─── Customer Account Roles ───────────────────────────────────────────────
    public const string CUSTOMER_MAIN = "CUSTOMER_MAIN";
    public const string CUSTOMER_PARTNER = "CUSTOMER_PARTNER";
    public const string CUSTOMER_SUB = "CUSTOMER_SUB";
    public const string CUSTOMER_CHILD = "CUSTOMER_CHILD";
}
