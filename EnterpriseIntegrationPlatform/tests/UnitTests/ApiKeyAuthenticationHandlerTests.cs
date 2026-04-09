using System.Security.Claims;
using System.Text.Encodings.Web;
using EnterpriseIntegrationPlatform.Admin.Api;
using EnterpriseIntegrationPlatform.Admin.Api.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class ApiKeyAuthenticationHandlerTests
{
    private const string ValidKey = "super-secret-key-12345";
    private const string ShortKey = "abc";

    private ApiKeyAuthenticationHandler CreateHandler(
        HttpContext context,
        IReadOnlyList<string>? configuredKeys = null)
    {
        configuredKeys ??= [ValidKey];

        var adminOptions = Options.Create(new AdminApiOptions
        {
            ApiKeys = configuredKeys,
        });

        var optionsMonitor = new TestOptionsMonitor();
        var loggerFactory = NullLoggerFactory.Instance;

        var handler = new ApiKeyAuthenticationHandler(
            optionsMonitor,
            loggerFactory,
            UrlEncoder.Default,
            adminOptions);

        // Initialize the handler with the scheme and context
        handler.InitializeAsync(
            new AuthenticationScheme(
                ApiKeyAuthenticationHandler.SchemeName,
                displayName: null,
                typeof(ApiKeyAuthenticationHandler)),
            context).GetAwaiter().GetResult();

        return handler;
    }

    private static DefaultHttpContext CreateHttpContext(string? apiKey = null)
    {
        var context = new DefaultHttpContext();
        if (apiKey is not null)
        {
            context.Request.Headers["X-Api-Key"] = apiKey;
        }
        return context;
    }

    [Test]
    public async Task HandleAuthenticate_MissingHeader_ReturnsFail()
    {
        var context = CreateHttpContext(apiKey: null);
        var handler = CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure?.Message, Does.Contain("Missing"));
    }

    [Test]
    public async Task HandleAuthenticate_InvalidKey_ReturnsFail()
    {
        var context = CreateHttpContext("wrong-key");
        var handler = CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure?.Message, Does.Contain("Invalid"));
    }

    [Test]
    public async Task HandleAuthenticate_ValidKey_ReturnsSuccess()
    {
        var context = CreateHttpContext(ValidKey);
        var handler = CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Ticket, Is.Not.Null);
    }

    [Test]
    public async Task HandleAuthenticate_ValidKey_SetsAdminRole()
    {
        var context = CreateHttpContext(ValidKey);
        var handler = CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.That(result.Succeeded, Is.True);
        var roleClaim = result.Principal!.FindFirst(ClaimTypes.Role);
        Assert.That(roleClaim, Is.Not.Null);
        Assert.That(roleClaim!.Value, Is.EqualTo("Admin"));
    }

    [Test]
    public async Task HandleAuthenticate_ValidKey_SetsNameClaim()
    {
        var context = CreateHttpContext(ValidKey);
        var handler = CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.That(result.Succeeded, Is.True);
        var nameClaim = result.Principal!.FindFirst(ClaimTypes.Name);
        Assert.That(nameClaim, Is.Not.Null);
        Assert.That(nameClaim!.Value, Is.EqualTo("admin"));
    }

    [Test]
    public async Task HandleAuthenticate_ValidKey_SetsApiKeyPrefixClaim()
    {
        var context = CreateHttpContext(ValidKey);
        var handler = CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.That(result.Succeeded, Is.True);
        var prefixClaim = result.Principal!.FindFirst("apikey_prefix");
        Assert.That(prefixClaim, Is.Not.Null);
        // Key "super-secret-key-12345" → first 4 chars = "supe" + "****"
        Assert.That(prefixClaim!.Value, Is.EqualTo("supe****"));
    }

    [Test]
    public async Task HandleAuthenticate_MultipleConfiguredKeys_AcceptsAny()
    {
        var context = CreateHttpContext("key-beta-67890");
        var handler = CreateHandler(context,
            configuredKeys: ["key-alpha-12345", "key-beta-67890"]);

        var result = await handler.AuthenticateAsync();

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public async Task HandleAuthenticate_CaseSensitive_RejectsWrongCase()
    {
        var context = CreateHttpContext(ValidKey.ToUpperInvariant());
        var handler = CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public async Task HandleAuthenticate_EmptyConfiguredKeys_RejectsAll()
    {
        var context = CreateHttpContext(ValidKey);
        var handler = CreateHandler(context, configuredKeys: []);

        var result = await handler.AuthenticateAsync();

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public async Task HandleAuthenticate_ShortKey_MasksEntirely()
    {
        var context = CreateHttpContext(ShortKey);
        var handler = CreateHandler(context, configuredKeys: [ShortKey]);

        var result = await handler.AuthenticateAsync();

        Assert.That(result.Succeeded, Is.True);
        var prefixClaim = result.Principal!.FindFirst("apikey_prefix");
        Assert.That(prefixClaim, Is.Not.Null);
        Assert.That(prefixClaim!.Value, Is.EqualTo("****"));
    }

    /// <summary>
    /// Minimal IOptionsMonitor implementation for AuthenticationSchemeOptions.
    /// </summary>
    private sealed class TestOptionsMonitor : IOptionsMonitor<AuthenticationSchemeOptions>
    {
        public AuthenticationSchemeOptions CurrentValue { get; } = new();
        public AuthenticationSchemeOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AuthenticationSchemeOptions, string?> listener) => null;
    }
}
