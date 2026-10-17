using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Krizovka.Users.Application.Core.Interfaces.Authentication;
using Krizovka.Users.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Krizovka.Users.Infrastructure.Authentication;

public class BackofficeTokenService : IBackofficeTokenService
{
    private readonly IConfiguration _configuration;

    public BackofficeTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(BackofficeUser user)
    {
        var secretKey = _configuration["BackofficeAuth:GlobalSecret"]
            ?? throw new InvalidOperationException("BackofficeAuth:GlobalSecret is not configured. Set BackofficeAuth__GlobalSecret environment variable.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(user.Role?.Code))
        {
            claims.Add(new Claim("role", user.Role.Code));
        }

        if (user.OrganizationId.HasValue)
        {
            claims.Add(new Claim("OrganizationId", user.OrganizationId.Value.ToString()));
            claims.Add(new Claim("org_id", user.OrganizationId.Value.ToString()));
        }

        if (user.IsSuperAdmin || user.Role?.Code == "SUPER_ADMIN" || user.Role?.Code == "BO_SUPER_ADMIN")
        {
            claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _configuration["BackofficeAuth:Issuer"] ?? "krizovka-users",
            Audience = _configuration["BackofficeAuth:Audience"] ?? "krizovka-admin",
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
