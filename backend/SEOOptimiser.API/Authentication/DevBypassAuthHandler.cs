using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SEOOptimiser.API.Authentication;

/// <summary>
/// Bypasses JWT validation and injects a synthetic dev user.
/// Only registered when Auth:Bypass = true — never in production.
/// </summary>
public class DevBypassAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "DevBypass";
    private const string DevUserId = "dev-user-bypass";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Logger.LogWarning(
            "Auth bypass is ENABLED — all requests are authenticated as '{UserId}'. " +
            "Never set Auth:Bypass=true in production.",
            DevUserId);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, DevUserId) };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
