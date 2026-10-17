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

        // Mobile identity provider is not enabled in the initial Krizovka deployment.
        if (!_configuration.GetValue<bool>("EndUserIdentity:Enabled"))
            return new ContextValidationResult { Allowed = false };

        // --- KRATOS FALLBACK (User App Tokens) ---
        // Backoffice no longer uses Kratos, but US still provides token validation for user app tokens
        HttpClient client;
        HttpRequestMessage requestMessage;

        if (Guid.TryParse(cleanToken, out _))
        {
            // Case 1: Session ID (Mobile/API)
            client = _httpClientFactory.CreateClient("KratosAdmin");
            requestMessage = new HttpRequestMessage(HttpMethod.Get, $"/admin/sessions/{cleanToken}?expand=identity");
        }
        else if (cleanToken.StartsWith("ory_st_"))
        {
            // Case 2: Session Token (Native Mobile App)
            client = _httpClientFactory.CreateClient("KratosPublic");
            requestMessage = new HttpRequestMessage(HttpMethod.Get, "/sessions/whoami");
            requestMessage.Headers.Add("X-Session-Token", cleanToken);
        }
        else
        {
            // Case 3: Kratos web cookie (Ory Session).
            // NOTE: Public "Kratos" HttpClient was removed since US is primarily Backoffice.
            // If we receive an opaque Kratos cookie here, we can't validate it without the public Kratos client.
            // But User App web UI doesn't use US for validation (it uses Gateway).
            _logger.LogWarning("Unsupported user session token format.");
            return new ContextValidationResult { Allowed = false };
        }

        try
        {
            var response = await client.SendAsync(requestMessage, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Kratos validation failed. Status: {StatusCode}", response.StatusCode);
                return new ContextValidationResult { Allowed = false };
            }

            var session = await response.Content.ReadFromJsonAsync<KratosSession>(_jsonOptions, cancellationToken);

            // Session Validation (Active & Expiry)
            if (session == null)
            {
                 _logger.LogWarning("Kratos session is null");
                 return new ContextValidationResult { Allowed = false };
            }

            if (session.Active != true)
            {
                _logger.LogWarning("Kratos session is not active");
                return new ContextValidationResult { Allowed = false };
            }

            if (session.ExpiresAt.HasValue && session.ExpiresAt.Value < DateTimeOffset.UtcNow)
            {
                _logger.LogWarning("Kratos session has expired at {ExpiresAt}", session.ExpiresAt);
                return new ContextValidationResult { Allowed = false };
            }

            if (session.Identity?.Id == null)
            {
                _logger.LogWarning("Kratos session identity ID is null");
                return new ContextValidationResult { Allowed = false };
            }

            if (!Guid.TryParse(session.Identity.Id, out var identityId))
            {
                _logger.LogWarning("Failed to parse Identity ID: {IdentityId}", session.Identity.Id);
                return new ContextValidationResult { Allowed = false };
            }

            var account = await _accountRepository.GetByIdentityAsync(identityId, cancellationToken);

            // Fallback: Auto-create account if missing (webhook might have failed)
            if (account == null)
            {
                var allowAutoCreate = _configuration.GetValue<bool>("ALLOW_ACCOUNT_AUTO_CREATE", false);

                if (allowAutoCreate)
                {
                    _logger.LogWarning("Account not found for Identity {IdentityId}, creating automatically (webhook fallback)", identityId);

                    try
                    {
                        await _mediator.Send(new CreateAccountFromIdentityCommand(identityId, null), cancellationToken);

                        // Retry fetch after creation
                        account = await _accountRepository.GetByIdentityAsync(identityId, cancellationToken);

                        if (account != null)
                        {
                            _logger.LogInformation("Successfully created account {AccountId} for Identity {IdentityId}", account.AccountId, identityId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to auto-create account for Identity {IdentityId}", identityId);
                    }
                }
                else
                {
                    _logger.LogWarning("Account not found for Identity {IdentityId} and auto-create is DISABLED. Access denied.", identityId);
                    // Do not return here, simply proceed, account will be null and it will fail later or fall through to BO check
                }
            }

            if (account != null)
            {
                if (account.Status != AccountStatus.ACTIVE)
                {
                    _logger.LogWarning("UserAccount is not ACTIVE. Status: {Status}", account.Status);
                    return new ContextValidationResult { Allowed = false };
                }

                var assignments = await _roleAssignmentRepository.GetByAccountIdAsync(account.AccountId, cancellationToken);
                var now = DateTimeOffset.UtcNow;

                var activeRoles = assignments
                    .Where(a => a.Status == AccountStatus.ACTIVE
                        && a.ValidFrom <= now
                        && (a.ValidTo == null || a.ValidTo > now))
                    .ToList();

                var allowed = true;
                var primaryRole = activeRoles.FirstOrDefault();

                var isBackoffice = activeRoles.Any(a => a.RoleCode.StartsWith("BO_") || a.RoleCode.EndsWith("_ADMIN") || a.RoleCode.EndsWith("_MANAGER"));

                return new ContextValidationResult
                {
                    Allowed = allowed,
                    IsValid = allowed,
                    AccountId = account.AccountId,
                    IdentityId = identityId,
                    Operation = "ValidateToken",
                    IsBackoffice = isBackoffice,
                    Roles = activeRoles.Select(a => a.RoleCode).ToList(),
                    Claims = new List<string>(),
                    Scope = primaryRole != null ? new ScopeInfo
                    {
                        ScopeType = primaryRole.ScopeType.ToString(),
                        ScopeId = primaryRole.ScopeId
                    } : null,
                    ScopeAssignments = activeRoles
                        .Where(a => !string.IsNullOrWhiteSpace(a.ScopeId))
                        .Select(a => new ScopeAssignmentDto
                        {
                            ScopeType = a.ScopeType.ToString(),
                            ScopeId = a.ScopeId,
                            RoleCode = a.RoleCode
                        })
                        .DistinctBy(a => $"{a.ScopeType}:{a.ScopeId}:{a.RoleCode}")
                        .ToList()
                };
            }

            _logger.LogWarning("UserAccount not found for Identity: {IdentityId}", identityId);
            return new ContextValidationResult { Allowed = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during token validation");
            return new ContextValidationResult { Allowed = false };
        }
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

            return new ContextValidationResult
            {
                Allowed = true,
                IsValid = true,
                AccountId = boUser.Id,
                IdentityId = boUser.IdentityId ?? boUser.Id,
                ActingAccountId = actingAccountId,
                Operation = "ValidateToken",
                IsBackoffice = true,
                Roles = new List<string> { boUser.Role?.Code ?? "BO_USER" },
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

    private class KratosSession
    {
        [JsonPropertyName("active")]
        public bool? Active { get; set; }

        [JsonPropertyName("expires_at")]
        public DateTimeOffset? ExpiresAt { get; set; }

        [JsonPropertyName("identity")]
        public KratosIdentity? Identity { get; set; }
    }

    private class KratosIdentity
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
