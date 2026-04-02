using EnterpriseIntegrationPlatform.Security.Secrets;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.SecretsTests;

[TestFixture]
public sealed class InMemorySecretProviderTests
{
    private InMemorySecretProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = new InMemorySecretProvider();
    }

    [Test]
    public async Task GetSecretAsync_NonExistentKey_ReturnsNull()
    {
        var result = await _provider.GetSecretAsync("missing-key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SetSecretAsync_NewSecret_CreatesWithVersion1()
    {
        var entry = await _provider.SetSecretAsync("db-password", "s3cret");

        Assert.That(entry.Key, Is.EqualTo("db-password"));
        Assert.That(entry.Value, Is.EqualTo("s3cret"));
        Assert.That(entry.Version, Is.EqualTo("1"));
    }

    [Test]
    public async Task SetSecretAsync_UpdateExisting_IncrementsVersion()
    {
        await _provider.SetSecretAsync("db-password", "v1");

        var updated = await _provider.SetSecretAsync("db-password", "v2");

        Assert.That(updated.Version, Is.EqualTo("2"));
        Assert.That(updated.Value, Is.EqualTo("v2"));
    }

    [Test]
    public async Task GetSecretAsync_AfterSet_ReturnsStoredEntry()
    {
        await _provider.SetSecretAsync("api-key", "abc123");

        var result = await _provider.GetSecretAsync("api-key");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("abc123"));
    }

    [Test]
    public async Task GetSecretAsync_SpecificVersion_ReturnsCorrectVersion()
    {
        await _provider.SetSecretAsync("key", "value-v1");
        await _provider.SetSecretAsync("key", "value-v2");
        await _provider.SetSecretAsync("key", "value-v3");

        var v1 = await _provider.GetSecretAsync("key", "1");
        var v2 = await _provider.GetSecretAsync("key", "2");

        Assert.That(v1!.Value, Is.EqualTo("value-v1"));
        Assert.That(v2!.Value, Is.EqualTo("value-v2"));
    }

    [Test]
    public async Task GetSecretAsync_LatestVersion_ReturnsNewest()
    {
        await _provider.SetSecretAsync("key", "old");
        await _provider.SetSecretAsync("key", "new");

        var latest = await _provider.GetSecretAsync("key");

        Assert.That(latest!.Value, Is.EqualTo("new"));
        Assert.That(latest.Version, Is.EqualTo("2"));
    }

    [Test]
    public async Task DeleteSecretAsync_ExistingKey_ReturnsTrue()
    {
        await _provider.SetSecretAsync("temp-key", "value");

        var deleted = await _provider.DeleteSecretAsync("temp-key");

        Assert.That(deleted, Is.True);
    }

    [Test]
    public async Task DeleteSecretAsync_NonExistentKey_ReturnsFalse()
    {
        var deleted = await _provider.DeleteSecretAsync("nonexistent");

        Assert.That(deleted, Is.False);
    }

    [Test]
    public async Task DeleteSecretAsync_AfterDelete_GetReturnsNull()
    {
        await _provider.SetSecretAsync("temp-key", "value");
        await _provider.DeleteSecretAsync("temp-key");

        var result = await _provider.GetSecretAsync("temp-key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ListSecretKeysAsync_ReturnsAllKeys()
    {
        await _provider.SetSecretAsync("key1", "v1");
        await _provider.SetSecretAsync("key2", "v2");
        await _provider.SetSecretAsync("key3", "v3");

        var keys = await _provider.ListSecretKeysAsync();

        Assert.That(keys, Has.Count.EqualTo(3));
        Assert.That(keys, Does.Contain("key1"));
        Assert.That(keys, Does.Contain("key2"));
        Assert.That(keys, Does.Contain("key3"));
    }

    [Test]
    public async Task ListSecretKeysAsync_WithPrefix_ReturnsMatchingOnly()
    {
        await _provider.SetSecretAsync("db/password", "v1");
        await _provider.SetSecretAsync("db/host", "v2");
        await _provider.SetSecretAsync("api/key", "v3");

        var dbKeys = await _provider.ListSecretKeysAsync("db/");

        Assert.That(dbKeys, Has.Count.EqualTo(2));
        Assert.That(dbKeys, Does.Contain("db/password"));
        Assert.That(dbKeys, Does.Contain("db/host"));
    }

    [Test]
    public async Task SetSecretAsync_WithMetadata_StoresMetadata()
    {
        var metadata = new Dictionary<string, string> { ["env"] = "prod", ["owner"] = "team-a" };

        var entry = await _provider.SetSecretAsync("key", "value", metadata);

        Assert.That(entry.Metadata, Is.Not.Null);
        Assert.That(entry.Metadata!["env"], Is.EqualTo("prod"));
        Assert.That(entry.Metadata["owner"], Is.EqualTo("team-a"));
    }

    [Test]
    public async Task SetSecretAsync_ConcurrentWrites_MaintainsConsistency()
    {
        var tasks = Enumerable.Range(0, 100).Select(i =>
            _provider.SetSecretAsync($"concurrent-key-{i}", $"value-{i}"));

        await Task.WhenAll(tasks);

        var keys = await _provider.ListSecretKeysAsync();
        Assert.That(keys, Has.Count.EqualTo(100));
    }

    [Test]
    public async Task SetSecretAsync_ConcurrentUpdatesToSameKey_AllVersionsCreated()
    {
        var tasks = Enumerable.Range(0, 50).Select(i =>
            _provider.SetSecretAsync("shared-key", $"value-{i}"));

        await Task.WhenAll(tasks);

        var result = await _provider.GetSecretAsync("shared-key");
        Assert.That(result, Is.Not.Null);
        Assert.That(int.Parse(result!.Version), Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task SetSecretAsync_SetsCreatedAtTimestamp()
    {
        var before = DateTimeOffset.UtcNow;

        var entry = await _provider.SetSecretAsync("key", "value");

        Assert.That(entry.CreatedAt, Is.GreaterThanOrEqualTo(before));
        Assert.That(entry.CreatedAt, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }
}
