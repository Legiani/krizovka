using Krizovka.Users.Application.Core.Interfaces.Services;
using Krizovka.Users.Domain.Constants;
using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Infrastructure.Services;

public class RoleCatalogService : IRoleCatalogService
{
    private static readonly List<RoleDefinition> _roles = new()
    {
        // ─── Customer Roles ───────────────────────────────────────────────────
        new(RoleCodes.CUSTOMER_MAIN,    "CUSTOMER", new[] { ScopeType.GLOBAL }, "Main customer account"),
        new(RoleCodes.CUSTOMER_PARTNER, "CUSTOMER", new[] { ScopeType.GLOBAL }, "Partner customer account"),
        new(RoleCodes.CUSTOMER_SUB,     "CUSTOMER", new[] { ScopeType.GLOBAL }, "Sub customer account"),
        new(RoleCodes.CUSTOMER_CHILD,   "CUSTOMER", new[] { ScopeType.GLOBAL }, "Child customer account"),

        // ─── Transport Backoffice Roles ───────────────────────────────────────
        new(RoleCodes.BO_CARD_APPROVER,
            "BO",
            new[] { ScopeType.IDS, ScopeType.CITY, ScopeType.ORGANIZATION, ScopeType.CARRIER, ScopeType.PROVIDER },
            "Card approval authority"),

        new(RoleCodes.BO_TARIFF_ADMIN,
            "BO",
            new[] { ScopeType.IDS, ScopeType.CITY, ScopeType.PROVIDER },
            "Tariff administration – manages fare rules, zones and pricing for IDS/CITY/PROVIDER"),

        new(RoleCodes.BO_REPORT_VIEWER,
            "BO",
            new[] { ScopeType.IDS, ScopeType.CITY, ScopeType.ORGANIZATION, ScopeType.PROVIDER },
            "Read-only access to reports and ABT dashboards"),

        new(RoleCodes.INFOCENTER_AGENT,
            "BO",
            new[] { ScopeType.IDS },
            "Info-centre agent – customer support and order lookup within an IDS"),

        new(RoleCodes.CARRIER_STAFF,
            "BO",
            new[] { ScopeType.CARRIER, ScopeType.IDS },
            "Carrier/provider staff – limited operational access (schedules, driver assignments) within CARRIER or IDS scope"),

        // ─── Organisation Roles ───────────────────────────────────────────────
        new(RoleCodes.BENEFIT_ORGANIZATION_ADMIN,
            "BO_ORGANIZATION",
            new[] { ScopeType.ORGANIZATION },
            "Organization admin – full access to benefit organisation data"),

        new(RoleCodes.BENEFIT_ORGANIZATION_SUPPORT,
            "BO_ORGANIZATION",
            new[] { ScopeType.ORGANIZATION },
            "Organization support staff"),

        new(RoleCodes.BENEFIT_ORGANIZATION_FINANCE,
            "BO_ORGANIZATION",
            new[] { ScopeType.ORGANIZATION },
            "Organization finance staff – access to invoices and reporting"),
    };

    public List<RoleDefinition> GetAllRoles() => _roles;

    public RoleDefinition? GetRole(string roleCode) =>
        _roles.FirstOrDefault(r => r.RoleCode == roleCode);
}
