using EnterpriseIntegrationPlatform.Security.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.SecretsTests;

[TestFixture]
public sealed class SecretRotationServiceTests
{
    private ISecretProvider _provider = null!;
    private SecretAuditLogger _auditLogger = null!;
    private ILogger<SecretRotationService> _logger = null!;
    private IOptions<SecretsOptions> _options = null!;
    private SecretRotationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = Substitute.For<ISecretProvider>();
        _auditLogger = new SecretAuditLogger(Substitute.For<ILogger<SecretAuditLogger>>());
        _logger = Substitute.For<ILogger<SecretRotationService>>();
        _options = Options.Create(new SecretsOptions
        {
            RotationCheckInterval = TimeSpan.FromMilliseconds(50)
        });
        _service = new SecretRotationService(_provider, _auditLogger, _options, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _service.Dispose();
    }

    [Test]
    public async Task RegisterPolicyAsync_StoresPolicy()
    {
        var policy = new SecretRotationPolicy(TimeSpan.FromHours(24));

        await _service.RegisterPolicyAsync("db-password", policy);

        var retrieved = await _service.GetPolicyAsync("db-password");
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.RotationInterval, Is.EqualTo(TimeSpan.FromHours(24)));
    }

    [Test]
    public async Task UnregisterPolicyAsync_ExistingPolicy_ReturnsTrue()
    {
        await _service.RegisterPolicyAsync("key", new SecretRotationPolicy(TimeSpan.FromHours(1)));

        var removed = await _service.UnregisterPolicyAsync("key");

        Assert.That(removed, Is.True);
    }

    [Test]
    public async Task UnregisterPolicyAsync_NonExistentPolicy_ReturnsFalse()
    {
        var removed = await _service.UnregisterPolicyAsync("nonexistent");

        Assert.That(removed, Is.False);
    }

    [Test]
    public async Task GetPolicyAsync_NonExistentKey_ReturnsNull()
    {
        var policy = await _service.GetPolicyAsync("missing");

        Assert.That(policy, Is.Null);
    }

    [Test]
    public async Task RotateNowAsync_CreatesNewSecretVersion()
    {
        var existing = new SecretEntry("key", "old-value", "1", DateTimeOffset.UtcNow.AddHours(-1));
        _provider.GetSecretAsync("key", null, Arg.Any<CancellationToken>()).Returns(existing);
        _provider.SetSecretAsync("key", Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new SecretEntry("key", callInfo.ArgAt<string>(1), "2", DateTimeOffset.UtcNow));

        var rotated = await _service.RotateNowAsync("key");

        Assert.That(rotated.Version, Is.EqualTo("2"));
        Assert.That(rotated.Value, Is.Not.EqualTo("old-value"));
        await _provider.Received(1).SetSecretAsync("key", Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RotateNowAsync_GeneratesNonEmptyValue()
    {
        _provider.GetSecretAsync("key", null, Arg.Any<CancellationToken>())
            .Returns(new SecretEntry("key", "old", "1", DateTimeOffset.UtcNow));
        _provider.SetSecretAsync("key", Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new SecretEntry("key", callInfo.ArgAt<string>(1), "2", DateTimeOffset.UtcNow));

        var rotated = await _service.RotateNowAsync("key");

        Assert.That(rotated.Value, Is.Not.Null.And.Not.Empty);
        Assert.That(rotated.Value.Length, Is.GreaterThan(10));
    }

    [Test]
    public async Task RegisterPolicyAsync_OverwritesExistingPolicy()
    {
        await _service.RegisterPolicyAsync("key", new SecretRotationPolicy(TimeSpan.FromHours(1)));
        await _service.RegisterPolicyAsync("key", new SecretRotationPolicy(TimeSpan.FromHours(2)));

        var policy = await _service.GetPolicyAsync("key");
        Assert.That(policy!.RotationInterval, Is.EqualTo(TimeSpan.FromHours(2)));
    }

    [Test]
    public async Task UnregisterPolicyAsync_AfterUnregister_GetReturnsNull()
    {
        await _service.RegisterPolicyAsync("key", new SecretRotationPolicy(TimeSpan.FromHours(1)));
        await _service.UnregisterPolicyAsync("key");

        var policy = await _service.GetPolicyAsync("key");
        Assert.That(policy, Is.Null);
    }

    [Test]
    public async Task RotateNowAsync_PreservesMetadata()
    {
        var metadata = new Dictionary<string, string> { ["env"] = "prod" } as IReadOnlyDictionary<string, string>;
        var existing = new SecretEntry("key", "old", "1", DateTimeOffset.UtcNow, Metadata: metadata);
        _provider.GetSecretAsync("key", null, Arg.Any<CancellationToken>()).Returns(existing);
        _provider.SetSecretAsync("key", Arg.Any<string>(), metadata, Arg.Any<CancellationToken>())
            .Returns(callInfo => new SecretEntry("key", callInfo.ArgAt<string>(1), "2", DateTimeOffset.UtcNow, Metadata: metadata));

        await _service.RotateNowAsync("key");

        await _provider.Received(1).SetSecretAsync("key", Arg.Any<string>(), metadata, Arg.Any<CancellationToken>());
    }
}
