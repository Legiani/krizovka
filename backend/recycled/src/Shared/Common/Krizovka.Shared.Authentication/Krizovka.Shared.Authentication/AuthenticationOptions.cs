using Microsoft.AspNetCore.Authentication;

namespace Krizovka.Shared.Authentication;

public class AuthenticationOptions : AuthenticationSchemeOptions
{
    public string UsServiceUrl { get; set; } = string.Empty;
    public int CacheDurationSeconds { get; set; } = 60;
    public bool RequireAuthentication { get; set; } = true;
}
