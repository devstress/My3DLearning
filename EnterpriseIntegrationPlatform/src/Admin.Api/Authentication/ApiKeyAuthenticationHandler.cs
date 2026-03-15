using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Admin.Api.Authentication;

/// <summary>
/// ASP.NET Core authentication handler that validates the <c>X-Api-Key</c> request
/// header against the configured list of authorised keys.
/// On success it creates a <see cref="ClaimsPrincipal"/> with the <see cref="AdminRole"/>
/// role so that standard <see cref="AuthorizeAttribute"/> / <c>RequireAuthorization()</c>
/// policies work without additional infrastructure.
/// </summary>
internal sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IOptions<AdminApiOptions> adminOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    /// <summary>The authentication scheme name registered in the DI container.</summary>
    public const string SchemeName = "ApiKey";

    /// <summary>The role name granted to successfully authenticated callers.</summary>
    public const string AdminRole = "Admin";

    private const string ApiKeyHeader = "X-Api-Key";

    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeader, out var apiKeyValues))
        {
            return Task.FromResult(
                AuthenticateResult.Fail($"Missing {ApiKeyHeader} header."));
        }

        var providedKey = apiKeyValues.ToString();
        var configuredKeys = adminOptions.Value.ApiKeys;

        if (!configuredKeys.Contains(providedKey, StringComparer.Ordinal))
        {
            Logger.LogWarning(
                "Admin API access denied – invalid API key supplied from {RemoteIp}",
                Context.Connection.RemoteIpAddress);

            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, AdminRole),
            new Claim("apikey_prefix", MaskKey(providedKey)),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static string MaskKey(string key) =>
        key.Length > 4 ? $"{key[..4]}****" : "****";
}
