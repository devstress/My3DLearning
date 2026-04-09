using EnterpriseIntegrationPlatform.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class SecurityServiceExtensionsTests
{
    // ── AddInputSanitizer ──────────────────────────────────────────────

    [Test]
    public void AddInputSanitizer_RegistersIInputSanitizer()
    {
        var services = new ServiceCollection();
        services.AddInputSanitizer();
        using var provider = services.BuildServiceProvider();

        var sanitizer = provider.GetService<IInputSanitizer>();

        Assert.That(sanitizer, Is.Not.Null);
        Assert.That(sanitizer, Is.InstanceOf<InputSanitizer>());
    }

    [Test]
    public void AddInputSanitizer_NullServices_ThrowsArgumentNullException()
    {
        Assert.That(
            () => SecurityServiceExtensions.AddInputSanitizer(null!),
            Throws.ArgumentNullException);
    }

    // ── AddPayloadSizeGuard ────────────────────────────────────────────

    [Test]
    public void AddPayloadSizeGuard_RegistersIPayloadSizeGuard()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PayloadSize:MaxPayloadBytes"] = "2048",
            })
            .Build();

        services.AddPayloadSizeGuard(config);
        using var provider = services.BuildServiceProvider();

        var guard = provider.GetService<IPayloadSizeGuard>();
        Assert.That(guard, Is.Not.Null);
        Assert.That(guard, Is.InstanceOf<PayloadSizeGuard>());
    }

    [Test]
    public void AddPayloadSizeGuard_BindsOptions()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PayloadSize:MaxPayloadBytes"] = "4096",
            })
            .Build();

        services.AddPayloadSizeGuard(config);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<PayloadSizeOptions>>();
        Assert.That(options.Value.MaxPayloadBytes, Is.EqualTo(4096));
    }

    // ── AddPlatformJwtAuthentication ───────────────────────────────────

    [Test]
    public void AddPlatformJwtAuthentication_MissingSigningKey_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "test-issuer",
            })
            .Build();

        Assert.That(
            () => services.AddPlatformJwtAuthentication(config),
            Throws.InstanceOf<InvalidOperationException>()
                .With.Message.Contains("SigningKey"));
    }

    [Test]
    public void AddPlatformJwtAuthentication_NullServices_ThrowsArgumentNullException()
    {
        var config = new ConfigurationBuilder().Build();
        Assert.That(
            () => SecurityServiceExtensions.AddPlatformJwtAuthentication(null!, config),
            Throws.ArgumentNullException);
    }

    [Test]
    public void AddPlatformJwtAuthentication_NullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Assert.That(
            () => services.AddPlatformJwtAuthentication(null!),
            Throws.ArgumentNullException);
    }
}
