using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Krizovka.Shared.Authentication;

public class MestoPlusAuthenticationHandler : AuthenticationHandler<AuthenticationOptions>
{
    private readonly IUsAuthClient _authClient;

    public MestoPlusAuthenticationHandler(
        IOptionsMonitor<AuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IUsAuthClient authClient)
        : base(options, logger, encoder)
    {
        _authClient = authClient;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. Extract Bearer token from Authorization header or fallback to Backoffice cookie
        //    NOTE: Even though US uses its own JwtBearer scheme for backend auth,
        //    other services (IS, SS, MS) rely on THIS shared handler to extract the session cookie
        //    forwarded by the Next.js BFF proxy.
        string? token = null;
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        bool isAuthApiMode = Request.Headers.TryGetValue("X-Auth-Mode", out var authMode) && authMode == "api";


        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = authHeader.Substring("Bearer ".Length).Trim();
        }
        else if (!isAuthApiMode && string.IsNullOrEmpty(authHeader))
        {
            // ODKAZ: Architektonický plán - Token Translation (Token Exchange na okraji sítě)
            // C# mikroslužby již NIKDY nepřijímají surové cookies přes interní kanály.
            // Next.js (API Gateway) se stará o konverzi `admin_session` do hlavičky Authorization: Bearer <JWT>.
            token = null;
        }

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        // 2. Validate token with US service
        var validationResult = await _authClient.ValidateContextAsync(token, Context.RequestAborted);

        if (validationResult == null)
        {
            return AuthenticateResult.Fail("Invalid or expired token");
        }

        if (Scheme.Name == "Backoffice" && !validationResult.IsBackoffice)
        {
            Logger.LogWarning("Rejected non-backoffice context for Backoffice scheme");
            return AuthenticateResult.Fail("Token is not valid for backoffice access");
        }

        // 3. Create Claims and Principal
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, validationResult.IdentityId.ToString()),
            new Claim("AccountId", validationResult.AccountId.ToString()),
            new Claim("IdentityId", validationResult.IdentityId.ToString()),
            new Claim("IsBackoffice", validationResult.IsBackoffice.ToString())
        };

        if (validationResult.ActingAccountId.HasValue)
        {
            claims.Add(new Claim("ActingAccountId", validationResult.ActingAccountId.Value.ToString()));
        }

        // Add roles
        foreach (var role in validationResult.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add custom claims (permissions)
        foreach (var permission in validationResult.Claims)
        {
            claims.Add(new Claim("Permission", permission));
        }

        if (validationResult.OrganizationId.HasValue)
        {
            if (!claims.Any(c => c.Type == "OrganizationId"))
            {
                claims.Add(new Claim("OrganizationId", validationResult.OrganizationId.Value.ToString()));
            }
            if (!claims.Any(c => c.Type == "org_id"))
            {
                claims.Add(new Claim("org_id", validationResult.OrganizationId.Value.ToString()));
            }
        }

        // If the token is a JWT, also extract OrganizationId, org_id, and SuperAdmin role if present
        var jwtHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        if (jwtHandler.CanReadToken(token))
        {
            var jwt = jwtHandler.ReadJwtToken(token);
            foreach (var claim in jwt.Claims)
            {
                if (claim.Type is "OrganizationId" or "org_id" or "organization_id")
                {
                    if (!claims.Any(c => c.Type == claim.Type))
                    {
                        claims.Add(new Claim(claim.Type, claim.Value));
                    }
                }
                else if (claim.Type == ClaimTypes.Role || claim.Type == "role")
                {
                    if (claim.Value.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)
                        && !claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "SuperAdmin"))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
                    }
                }
            }
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        // Inject headers for controllers that expect them (backward compatibility)
        Context.Items["AccountId"] = validationResult.AccountId;
        Context.Items["IdentityId"] = validationResult.IdentityId;
        Context.Items["Claims"] = validationResult.Claims;
        Context.Items["Roles"] = validationResult.Roles;

        // Expose Scope context for standard authorization mechanisms
        if (!string.IsNullOrEmpty(validationResult.ScopeType))
        {
            Context.Items["ScopeType"] = validationResult.ScopeType;
        }
        if (!string.IsNullOrEmpty(validationResult.ScopeId))
        {
            Context.Items["ScopeId"] = validationResult.ScopeId;
        }
        if (validationResult.CityAssignments?.Any() == true)
        {
            Context.Items["CityAssignments"] = validationResult.CityAssignments;
        }
        if (validationResult.ScopeAssignments?.Any() == true)
        {
            Context.Items["ScopeAssignments"] = validationResult.ScopeAssignments;
        }

        Request.Headers["X-User-Id"] = validationResult.IdentityId.ToString();
        Request.Headers["X-Account-Id"] = validationResult.AccountId.ToString();

        Logger.LogInformation("Authenticated request for account {AccountId} via {SchemeName}", validationResult.AccountId, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
