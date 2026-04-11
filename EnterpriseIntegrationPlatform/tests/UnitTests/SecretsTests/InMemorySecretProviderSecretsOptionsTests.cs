using EnterpriseIntegrationPlatform.Security.Secrets;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.SecretsTests;

[TestFixture]
public sealed class InMemorySecretProviderAuditTests
{
    private SecretAuditLogger _auditLogger = null!;
    private InMemorySecretProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _auditLogger = new SecretAuditLogger(Substitute.For<ILogger<SecretAuditLogger>>());
        _provider = new InMemorySecretProvider(_auditLogger);
    }

    [Test]
    public async Task GetSecretAsync_WithInvalidVersion_ReturnsLatest()
    {
        await _provider.SetSecretAsync("key", "latest");
        var result = await _provider.GetSecretAsync("key", "not-a-number");

        Assert.That(result!.Value, Is.EqualTo("latest"));
    }

    [Test]
    public void GetSecretAsync_NullKey_Throws()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _provider.GetSecretAsync(null!));
    }

    [Test]
    public void SetSecretAsync_NullKey_Throws()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _provider.SetSecretAsync(null!, "v"));
    }

    [Test]
    public void SetSecretAsync_NullValue_Throws()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _provider.SetSecretAsync("k", null!));
    }

    [Test]
    public void DeleteSecretAsync_EmptyKey_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _provider.DeleteSecretAsync(""));
    }

    [Test]
    public void Constructor_WithoutAuditLogger_DoesNotThrow()
    {
        var provider = new InMemorySecretProvider();
        Assert.That(provider, Is.Not.Null);
    }
}

[TestFixture]
public sealed class SecretsOptionsTests
{
    [Test]
    public void Defaults_AreCorrect()
    {
        var options = new SecretsOptions();

        Assert.That(options.Provider, Is.EqualTo("InMemory"));
        Assert.That(options.VaultMountPath, Is.EqualTo("secret"));
        Assert.That(options.CacheTtl, Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(options.RotationCheckInterval, Is.EqualTo(TimeSpan.FromMinutes(1)));
        Assert.That(options.EnableAuditLogging, Is.True);
    }

    [Test]
    public void SectionName_IsSecrets()
    {
        Assert.That(SecretsOptions.SectionName, Is.EqualTo("Secrets"));
    }

    [Test]
    public void AzureProperties_DefaultToNull()
    {
        var options = new SecretsOptions();

        Assert.That(options.AzureKeyVaultUri, Is.Null);
        Assert.That(options.AzureTenantId, Is.Null);
        Assert.That(options.AzureClientId, Is.Null);
        Assert.That(options.AzureClientSecret, Is.Null);
    }

    [Test]
    public void VaultProperties_DefaultToNull()
    {
        var options = new SecretsOptions();

        Assert.That(options.VaultAddress, Is.Null);
        Assert.That(options.VaultToken, Is.Null);
    }

    [Test]
    public void SettableProperties_RoundTrip()
    {
        var options = new SecretsOptions
        {
            Provider = "Vault",
            VaultAddress = "https://vault:8200",
            VaultToken = "token",
            VaultMountPath = "kv",
            AzureKeyVaultUri = "https://myvault.vault.azure.net",
            CacheTtl = TimeSpan.FromMinutes(10),
            RotationCheckInterval = TimeSpan.FromSeconds(30),
            EnableAuditLogging = false,
        };

        Assert.That(options.Provider, Is.EqualTo("Vault"));
        Assert.That(options.VaultAddress, Is.EqualTo("https://vault:8200"));
        Assert.That(options.VaultMountPath, Is.EqualTo("kv"));
        Assert.That(options.CacheTtl.TotalMinutes, Is.EqualTo(10));
        Assert.That(options.EnableAuditLogging, Is.False);
    }
}

[TestFixture]
public sealed class SecretEntryTests
{
    [Test]
    public void IsExpired_NotExpired_ReturnsFalse()
    {
        var entry = new SecretEntry("key", "val", "1", DateTimeOffset.UtcNow,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1));

        Assert.That(entry.IsExpired, Is.False);
    }

    [Test]
    public void IsExpired_Expired_ReturnsTrue()
    {
        var entry = new SecretEntry("key", "val", "1", DateTimeOffset.UtcNow,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(-1));

        Assert.That(entry.IsExpired, Is.True);
    }

    [Test]
    public void IsExpired_NoExpiry_ReturnsFalse()
    {
        var entry = new SecretEntry("key", "val", "1", DateTimeOffset.UtcNow);

        Assert.That(entry.IsExpired, Is.False);
    }

    [Test]
    public void Record_Equality_SameValues_AreEqual()
    {
        var ts = DateTimeOffset.UtcNow;
        var a = new SecretEntry("k", "v", "1", ts);
        var b = new SecretEntry("k", "v", "1", ts);

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void Record_Equality_DifferentValues_AreNotEqual()
    {
        var ts = DateTimeOffset.UtcNow;
        var a = new SecretEntry("k", "v1", "1", ts);
        var b = new SecretEntry("k", "v2", "1", ts);

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Record_WithExpression_ChangesValue()
    {
        var entry = new SecretEntry("k", "old", "1", DateTimeOffset.UtcNow);
        var updated = entry with { Value = "new", Version = "2" };

        Assert.That(updated.Value, Is.EqualTo("new"));
        Assert.That(updated.Version, Is.EqualTo("2"));
        Assert.That(updated.Key, Is.EqualTo("k"));
    }

    [Test]
    public void Metadata_CanBeNull()
    {
        var entry = new SecretEntry("k", "v", "1", DateTimeOffset.UtcNow);
        Assert.That(entry.Metadata, Is.Null);
    }

    [Test]
    public void Metadata_CanHaveValues()
    {
        var meta = new Dictionary<string, string> { ["env"] = "prod" };
        var entry = new SecretEntry("k", "v", "1", DateTimeOffset.UtcNow, Metadata: meta);

        Assert.That(entry.Metadata, Is.Not.Null);
        Assert.That(entry.Metadata!["env"], Is.EqualTo("prod"));
    }
}

[TestFixture]
public sealed class SecretAuditEventTests
{
    [Test]
    public void Record_AllProperties_SetCorrectly()
    {
        var ts = DateTimeOffset.UtcNow;
        var evt = new SecretAuditEvent(
            SecretAccessAction.Read, "my-key", ts,
            Principal: "admin", Version: "3", Success: true, Detail: "ok");

        Assert.That(evt.Action, Is.EqualTo(SecretAccessAction.Read));
        Assert.That(evt.SecretKey, Is.EqualTo("my-key"));
        Assert.That(evt.Timestamp, Is.EqualTo(ts));
        Assert.That(evt.Principal, Is.EqualTo("admin"));
        Assert.That(evt.Version, Is.EqualTo("3"));
        Assert.That(evt.Success, Is.True);
        Assert.That(evt.Detail, Is.EqualTo("ok"));
    }

    [Test]
    public void Record_DefaultOptionals_AreCorrect()
    {
        var evt = new SecretAuditEvent(SecretAccessAction.Write, "key", DateTimeOffset.UtcNow);

        Assert.That(evt.Principal, Is.Null);
        Assert.That(evt.Version, Is.Null);
        Assert.That(evt.Success, Is.True);
        Assert.That(evt.Detail, Is.Null);
    }
}

[TestFixture]
public sealed class SecretAccessActionTests
{
    [Test]
    public void AllValues_AreDefined()
    {
        var values = Enum.GetValues<SecretAccessAction>();

        Assert.That(values, Has.Length.EqualTo(7));
        Assert.That(values, Does.Contain(SecretAccessAction.Read));
        Assert.That(values, Does.Contain(SecretAccessAction.Write));
        Assert.That(values, Does.Contain(SecretAccessAction.Delete));
        Assert.That(values, Does.Contain(SecretAccessAction.List));
        Assert.That(values, Does.Contain(SecretAccessAction.Rotate));
        Assert.That(values, Does.Contain(SecretAccessAction.CacheHit));
        Assert.That(values, Does.Contain(SecretAccessAction.CacheEvict));
    }
}

[TestFixture]
public sealed class SecretRotationPolicyTests
{
    [Test]
    public void Record_AllProperties_SetCorrectly()
    {
        var policy = new SecretRotationPolicy(
            TimeSpan.FromDays(30),
            AutoRotate: true,
            RotateBeforeExpiry: TimeSpan.FromDays(7),
            NotifyOnRotation: false);

        Assert.That(policy.RotationInterval, Is.EqualTo(TimeSpan.FromDays(30)));
        Assert.That(policy.AutoRotate, Is.True);
        Assert.That(policy.RotateBeforeExpiry, Is.EqualTo(TimeSpan.FromDays(7)));
        Assert.That(policy.NotifyOnRotation, Is.False);
    }

    [Test]
    public void Record_DefaultOptionals_AreCorrect()
    {
        var policy = new SecretRotationPolicy(TimeSpan.FromHours(1));

        Assert.That(policy.AutoRotate, Is.True);
        Assert.That(policy.RotateBeforeExpiry, Is.Null);
        Assert.That(policy.NotifyOnRotation, Is.True);
    }
}
