using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Krizovka.Users.Application.Core.Identities.Commands.CreateAccountFromIdentity;
using Krizovka.Users.Application.Core.Context.DTOs;
using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Enums;
using Krizovka.Users.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace Krizovka.Users.Application.Core.Context.Queries.ValidateContext;

public class ValidateTokenQueryHandler : IRequestHandler<ValidateTokenQuery, ContextValidationResult>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUserAccountRepository _accountRepository;
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IBackofficeUserRepository _backofficeUserRepository;
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly Microsoft.Extensions.Logging.ILogger<ValidateTokenQueryHandler> _logger;
    private readonly IMemoryCache _memoryCache;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ValidateTokenQueryHandler(
        IHttpClientFactory httpClientFactory,
        IUserAccountRepository accountRepository,
        IRoleAssignmentRepository roleAssignmentRepository,
        IBackofficeUserRepository backofficeUserRepository,
        IMediator mediator,
        Microsoft.Extensions.Logging.ILogger<ValidateTokenQueryHandler> logger,
        IConfiguration configuration,
        Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache)
    {
        _httpClientFactory = httpClientFactory;
        _accountRepository = accountRepository;
        _roleAssignmentRepository = roleAssignmentRepository;
        _backofficeUserRepository = backofficeUserRepository;
        _mediator = mediator;
        _logger = logger;
        _configuration = configuration;
        _memoryCache = memoryCache;
    }

    public async Task<ContextValidationResult> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        // Cache Key
        var cacheKey = "AuthToken:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(request.Token ?? "")));
        if (_memoryCache.TryGetValue(cacheKey, out ContextValidationResult? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        var result = await InternalHandle(request, cancellationToken);

        // Cache valid or even invalid results?
        // User request says: "mapu token -> ContextValidationResult na 30–60s" using MemoryCache.
        // It's safer to cache only non-sensitive data, but ContextValidationResult is specifically for Auth.
        // We will cache it for 30s.

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(30));

        _memoryCache.Set(cacheKey, result, cacheEntryOptions);

        return result;
    }

    private async Task<ContextValidationResult> InternalHandle(ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        var cleanToken = request.Token?.Trim();
        if (string.IsNullOrEmpty(cleanToken))
        {
            return new ContextValidationResult { Allowed = false };
        }

        // 1. Is it a JWT? Backoffice tokens are always standard JWTs.
        // CanReadToken can sometimes be overly strict. A quick check is if it has 3 parts separated by dots.
        var tokenHandler = new JwtSecurityTokenHandler();
        var isJwtFormat = cleanToken.Split('.').Length == 3;

        if (isJwtFormat && tokenHandler.CanReadToken(cleanToken))
        {
            return await ValidateBackofficeJwtAsync(cleanToken, cancellationToken);
        }

        return new ContextValidationResult { Allowed = false };
    }

    private async Task<ContextValidationResult> ValidateBackofficeJwtAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var secretKey = _configuration["BackofficeAuth:GlobalSecret"]
            ?? throw new InvalidOperationException("BackofficeAuth:GlobalSecret is not configured. Set BackofficeAuth__GlobalSecret environment variable.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var issuer = _configuration["BackofficeAuth:Issuer"] ?? "krizovka-users";
        var audience = _configuration["BackofficeAuth:Audience"] ?? "krizovka-admin";



        try
        {
            var claimsPrincipal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)  // Allow up to 5 min clock drift
            }, out _);

            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            _logger.LogDebug("[BackofficeJWT] Token validated. Sub/NameIdentifier={Sub}", userIdClaim);

            var actingAccountIdClaim = claimsPrincipal.FindFirst("ActingAccountId")?.Value;
            Guid? actingAccountId = null;
            if (!string.IsNullOrEmpty(actingAccountIdClaim) && Guid.TryParse(actingAccountIdClaim, out var parsedActingId))
            {
                actingAccountId = parsedActingId;
            }

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("[BackofficeJWT] Sub claim is not a valid GUID: {Sub}", userIdClaim);
                return new ContextValidationResult { Allowed = false };
            }

            var boUser = await _backofficeUserRepository.GetByIdAsync(userId, cancellationToken);
            if (boUser == null)
            {
                _logger.LogWarning("[BackofficeJWT] BackofficeUser not found for Id={UserId}", userId);
                return new ContextValidationResult { Allowed = false };
            }

            if (!boUser.IsActive)
            {
                _logger.LogWarning("[BackofficeJWT] Rejected inactive backoffice user Id={UserId}", userId);
                return new ContextValidationResult { Allowed = false };
            }

            _logger.LogInformation("[BackofficeJWT] Auth success for user {Email} Id={UserId}", boUser.Email, userId);

            var permissions = boUser.Role?.Permissions?
                .Select(p => $"{p.Module}:{p.Action}")
                .ToList() ?? new List<string>();

            // Determine Scope from Permissions
            var scopeType = "BACKOFFICE";
            var scopeId = "GLOBAL"; // Fallback
            var cityAssignments = new List<CityAssignmentDto>();
            var scopeAssignments = new List<ScopeAssignmentDto>();

            if (boUser.Role?.Permissions != null && boUser.Role.Permissions.Any())
            {
                scopeAssignments = boUser.Role.Permissions
                    .Where(p => !string.IsNullOrWhiteSpace(p.ScopeType) && !string.IsNullOrWhiteSpace(p.ScopeValue))
                    .Select(p => new ScopeAssignmentDto
                    {
                        ScopeType = p.ScopeType,
                        ScopeId = p.ScopeValue!,
                        RoleCode = boUser.Role.Code
                    })
                    .DistinctBy(a => $"{a.ScopeType}:{a.ScopeId}:{a.RoleCode}")
                    .ToList();

                // Find highest privilege scope (GLOBAL > CITY)
                var globalPerm = boUser.Role.Permissions.FirstOrDefault(p => p.ScopeType == "GLOBAL");
                if (globalPerm != null)
                {
                    scopeType = "GLOBAL";
                    scopeId = "*";
                    // Super-admin / global — cityAssignments stays empty (global access applies)
                }
                else
                {
                    // Collect all distinct CITY-scoped permissions as city assignments
                    cityAssignments = boUser.Role.Permissions
                        .Where(p => p.ScopeType == "CITY" && !string.IsNullOrEmpty(p.ScopeValue))
                        .Select(p => new CityAssignmentDto
                        {
                            CityId = p.ScopeValue!,
                            RoleCode = boUser.Role.Code
                        })
                        .DistinctBy(a => a.CityId)
                        .ToList();

                    var firstCity = cityAssignments.FirstOrDefault();
                    if (firstCity != null)
                    {
                        scopeType = "CITY";
                        scopeId = firstCity.CityId;
                    }
                }
            }

            var roles = new List<string> { boUser.Role?.Code ?? "BO_USER" };
            if (boUser.IsSuperAdmin || boUser.Role?.Code == RoleCodes.BO_SUPER_ADMIN || claimsPrincipal.IsInRole("SuperAdmin"))
            {
                if (!roles.Contains("SuperAdmin"))
                {
                    roles.Add("SuperAdmin");
                }
            }

            Guid? orgId = boUser.OrganizationId;
            if (!orgId.HasValue)
            {
                var orgClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value
                    ?? claimsPrincipal.FindFirst("org_id")?.Value;
                if (Guid.TryParse(orgClaim, out var parsedOrg))
                {
                    orgId = parsedOrg;
                }
            }

            return new ContextValidationResult
            {
                Allowed = true,
                IsValid = true,
                AccountId = boUser.Id,
                IdentityId = boUser.IdentityId ?? boUser.Id,
                ActingAccountId = actingAccountId,
                OrganizationId = orgId,
                Operation = "ValidateToken",
                IsBackoffice = true,
                Roles = roles,
                Claims = permissions,
                Scope = new ScopeInfo
                {
                    ScopeType = scopeType,
                    ScopeId = scopeId
                },
                CityAssignments = cityAssignments,
                ScopeAssignments = scopeAssignments
            };
        }
        catch (Exception ex)
        {
            // Log full exception type + message to diagnose the failure
            _logger.LogWarning(
                "[BackofficeJWT] Validation failed. Type={ExType} Message='{ExMsg}' Inner='{InnerMsg}' TokenLen={TokenLen}",
                ex.GetType().Name,
                ex.Message,
                ex.InnerException?.Message ?? "",
                token?.Length ?? 0);
            return new ContextValidationResult { Allowed = false };
        }
    }
}

