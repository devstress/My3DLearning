using EnterpriseIntegrationPlatform.Security.Secrets;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.SecretsTests;

[TestFixture]
public sealed class CachedSecretProviderTests
{
    private ISecretProvider _innerProvider = null!;
    private SecretAuditLogger _auditLogger = null!;
    private CachedSecretProvider _cached = null!;

    [SetUp]
    public void SetUp()
    {
        _innerProvider = Substitute.For<ISecretProvider>();
        _auditLogger = new SecretAuditLogger(Substitute.For<ILogger<SecretAuditLogger>>());
        _cached = new CachedSecretProvider(_innerProvider, TimeSpan.FromMinutes(5), _auditLogger);
    }

    [Test]
    public async Task GetSecretAsync_FirstCall_DelegatesToInner()
    {
        var entry = new SecretEntry("key", "value", "1", DateTimeOffset.UtcNow);
        _innerProvider.GetSecretAsync("key", null, Arg.Any<CancellationToken>()).Returns(entry);

        var result = await _cached.GetSecretAsync("key");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("value"));
        await _innerProvider.Received(1).GetSecretAsync("key", null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetSecretAsync_SecondCall_ReturnsCachedValue()
    {
        var entry = new SecretEntry("key", "value", "1", DateTimeOffset.UtcNow);
        _innerProvider.GetSecretAsync("key", null, Arg.Any<CancellationToken>()).Returns(entry);

        await _cached.GetSecretAsync("key");
        var result = await _cached.GetSecretAsync("key");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("value"));
        await _innerProvider.Received(1).GetSecretAsync("key", null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetSecretAsync_AfterTtlExpiry_FetchesAgain()
    {
        var shortTtl = new CachedSecretProvider(
            _innerProvider, TimeSpan.FromMilliseconds(50), _auditLogger);

        var entry = new SecretEntry("key", "value", "1", DateTimeOffset.UtcNow);
        _innerProvider.GetSecretAsync("key", null, Arg.Any<CancellationToken>()).Returns(entry);

        await shortTtl.GetSecretAsync("key");
        await Task.Delay(100);
        await shortTtl.GetSecretAsync("key");

        await _innerProvider.Received(2).GetSecretAsync("key", null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SetSecretAsync_InvalidatesCache()
    {
        var entry = new SecretEntry("key", "old-value", "1", DateTimeOffset.UtcNow);
        _innerProvider.GetSecretAsync("key", null, Arg.Any<CancellationToken>()).Returns(entry);
        _innerProvider.SetSecretAsync("key", "new-value", null, Arg.Any<CancellationToken>())
            .Returns(new SecretEntry("key", "new-value", "2", DateTimeOffset.UtcNow));

        await _cached.GetSecretAsync("key");
        await _cached.SetSecretAsync("key", "new-value");

        var updatedEntry = new SecretEntry("key", "new-value", "2", DateTimeOffset.UtcNow);
        _innerProvider.GetSecretAsync("key", null, Arg.Any<CancellationToken>()).Returns(updatedEntry);

        await _cached.GetSecretAsync("key");

        await _innerProvider.Received(2).GetSecretAsync("key", null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteSecretAsync_InvalidatesCache()
    {
        var entry = new SecretEntry("key", "value", "1", DateTimeOffset.UtcNow);
        _innerProvider.GetSecretAsync("key", null, Arg.Any<CancellationToken>()).Returns(entry);
        _innerProvider.DeleteSecretAsync("key", Arg.Any<CancellationToken>()).Returns(true);

        await _cached.GetSecretAsync("key");
        await _cached.DeleteSecretAsync("key");

        _innerProvider.GetSecretAsync("key", null, Arg.Any<CancellationToken>()).Returns((SecretEntry?)null);
        var result = await _cached.GetSecretAsync("key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetSecretAsync_NullResult_IsNotCached()
    {
        _innerProvider.GetSecretAsync("missing", null, Arg.Any<CancellationToken>())
            .Returns((SecretEntry?)null);

        await _cached.GetSecretAsync("missing");
        await _cached.GetSecretAsync("missing");

        await _innerProvider.Received(2).GetSecretAsync("missing", null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetSecretAsync_VersionedRequests_CachedSeparately()
    {
        var v1 = new SecretEntry("key", "v1-value", "1", DateTimeOffset.UtcNow);
        var v2 = new SecretEntry("key", "v2-value", "2", DateTimeOffset.UtcNow);
        _innerProvider.GetSecretAsync("key", "1", Arg.Any<CancellationToken>()).Returns(v1);
        _innerProvider.GetSecretAsync("key", "2", Arg.Any<CancellationToken>()).Returns(v2);

        var result1 = await _cached.GetSecretAsync("key", "1");
        var result2 = await _cached.GetSecretAsync("key", "2");

        Assert.That(result1!.Value, Is.EqualTo("v1-value"));
        Assert.That(result2!.Value, Is.EqualTo("v2-value"));
    }

    [Test]
    public void Constructor_ZeroTtl_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CachedSecretProvider(_innerProvider, TimeSpan.Zero));
    }

    [Test]
    public void Constructor_NegativeTtl_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CachedSecretProvider(_innerProvider, TimeSpan.FromSeconds(-1)));
    }

    [Test]
    public async Task Invalidate_ClearsAllCacheEntries()
    {
        var entry = new SecretEntry("key", "value", "1", DateTimeOffset.UtcNow);
        _innerProvider.GetSecretAsync("key", null, Arg.Any<CancellationToken>()).Returns(entry);

        await _cached.GetSecretAsync("key");
        _cached.Invalidate();
        await _cached.GetSecretAsync("key");

        await _innerProvider.Received(2).GetSecretAsync("key", null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListSecretKeysAsync_DelegatesToInner()
    {
        _innerProvider.ListSecretKeysAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "key1", "key2" });

        var keys = await _cached.ListSecretKeysAsync();

        Assert.That(keys, Has.Count.EqualTo(2));
    }
}
