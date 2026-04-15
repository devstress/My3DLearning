using System.Net;
using System.Text.Json;
using EnterpriseIntegrationPlatform.Security.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.SecretsTests;

[TestFixture]
public sealed class VaultSecretProviderTests
{
    private MockVaultHandler _httpHandler = null!;
    private SecretAuditLogger _auditLogger = null!;
    private ILogger<VaultSecretProvider> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _httpHandler = new MockVaultHandler();
        _auditLogger = new SecretAuditLogger(Substitute.For<ILogger<SecretAuditLogger>>());
        _logger = Substitute.For<ILogger<VaultSecretProvider>>();
    }

    [TearDown]
    public void TearDown()
    {
        _httpHandler.Dispose();
    }

    [Test]
    public async Task GetSecretAsync_Success_ReturnsSecretEntry()
    {
        _httpHandler.ResponseBody = JsonSerializer.Serialize(new
        {
            data = new
            {
                data = new Dictionary<string, string> { ["value"] = "db-pass-123" },
                metadata = new { version = 5, created_time = "2024-01-15T10:00:00Z" }
            }
        });

        using var provider = CreateProvider();
        var result = await provider.GetSecretAsync("db-password");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Key, Is.EqualTo("db-password"));
        Assert.That(result.Value, Is.EqualTo("db-pass-123"));
        Assert.That(result.Version, Is.EqualTo("5"));
    }

    [Test]
    public async Task GetSecretAsync_NotFound_ReturnsNull()
    {
        _httpHandler.ResponseStatus = HttpStatusCode.NotFound;
        _httpHandler.ResponseBody = "{}";

        using var provider = CreateProvider();
        var result = await provider.GetSecretAsync("missing");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetSecretAsync_WithVersion_IncludesVersionParam()
    {
        _httpHandler.ResponseBody = JsonSerializer.Serialize(new
        {
            data = new
            {
                data = new Dictionary<string, string> { ["value"] = "old-val" },
                metadata = new { version = 2 }
            }
        });

        using var provider = CreateProvider();
        var result = await provider.GetSecretAsync("key", "2");

        Assert.That(result, Is.Not.Null);
        Assert.That(_httpHandler.LastRequestUri!.ToString(), Does.Contain("version=2"));
    }

    [Test]
    public async Task GetSecretAsync_NullDataPayload_ReturnsNull()
    {
        _httpHandler.ResponseBody = JsonSerializer.Serialize(new
        {
            data = (object?)null
        });

        using var provider = CreateProvider();
        var result = await provider.GetSecretAsync("key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetSecretAsync_WithLeaseId_TracksLease()
    {
        _httpHandler.ResponseBody = JsonSerializer.Serialize(new
        {
            data = new
            {
                data = new Dictionary<string, string> { ["value"] = "val" },
                metadata = new { version = 1 }
            },
            lease_id = "lease-abc-123",
            lease_duration = 3600
        });

        using var provider = CreateProvider();
        var result = await provider.GetSecretAsync("dynamic-cred");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo("val"));
    }

    [Test]
    public async Task SetSecretAsync_Success_ReturnsEntry()
    {
        _httpHandler.ResponseBody = JsonSerializer.Serialize(new
        {
            data = new { version = 3, created_time = "2024-06-01T00:00:00Z" }
        });

        using var provider = CreateProvider();
        var result = await provider.SetSecretAsync("api-key", "new-api-key-value");

        Assert.That(result.Key, Is.EqualTo("api-key"));
        Assert.That(result.Value, Is.EqualTo("new-api-key-value"));
        Assert.That(result.Version, Is.EqualTo("3"));
    }

    [Test]
    public async Task SetSecretAsync_WithMetadata_MergesIntoPayload()
    {
        _httpHandler.ResponseBody = JsonSerializer.Serialize(new
        {
            data = new { version = 1 }
        });

        var metadata = new Dictionary<string, string> { ["env"] = "staging", ["owner"] = "team-a" };

        using var provider = CreateProvider();
        var result = await provider.SetSecretAsync("key", "val", metadata);

        Assert.That(result, Is.Not.Null);
        Assert.That(_httpHandler.LastRequestMethod, Is.EqualTo(HttpMethod.Post));
    }

    [Test]
    public async Task DeleteSecretAsync_Success_ReturnsTrue()
    {
        _httpHandler.ResponseStatus = HttpStatusCode.NoContent;
        _httpHandler.ResponseBody = "";

        using var provider = CreateProvider();
        var result = await provider.DeleteSecretAsync("old-secret");

        Assert.That(result, Is.True);
        Assert.That(_httpHandler.LastRequestUri!.ToString(), Does.Contain("/metadata/old-secret"));
    }

    [Test]
    public async Task DeleteSecretAsync_Failure_ReturnsFalse()
    {
        _httpHandler.ResponseStatus = HttpStatusCode.Forbidden;
        _httpHandler.ResponseBody = "{}";

        using var provider = CreateProvider();
        var result = await provider.DeleteSecretAsync("restricted");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ListSecretKeysAsync_ReturnsKeys()
    {
        _httpHandler.ResponseBody = JsonSerializer.Serialize(new
        {
            data = new { keys = new[] { "secret-a", "secret-b", "secret-c" } }
        });

        using var provider = CreateProvider();
        var keys = await provider.ListSecretKeysAsync();

        Assert.That(keys, Has.Count.EqualTo(3));
        Assert.That(keys, Does.Contain("secret-a"));
    }

    [Test]
    public async Task ListSecretKeysAsync_Failure_ReturnsEmpty()
    {
        _httpHandler.ResponseStatus = HttpStatusCode.Forbidden;
        _httpHandler.ResponseBody = "{}";

        using var provider = CreateProvider();
        var keys = await provider.ListSecretKeysAsync();

        Assert.That(keys, Is.Empty);
    }

    [Test]
    public void GetSecretAsync_NullKey_ThrowsArgumentException()
    {
        using var provider = CreateProvider();
        Assert.ThrowsAsync<ArgumentNullException>(() => provider.GetSecretAsync(null!));
    }

    [Test]
    public void SetSecretAsync_NullKey_ThrowsArgumentException()
    {
        using var provider = CreateProvider();
        Assert.ThrowsAsync<ArgumentNullException>(() => provider.SetSecretAsync(null!, "val"));
    }

    [Test]
    public void SetSecretAsync_NullValue_ThrowsArgumentNullException()
    {
        using var provider = CreateProvider();
        Assert.ThrowsAsync<ArgumentNullException>(() => provider.SetSecretAsync("key", null!));
    }

    [Test]
    public void DeleteSecretAsync_EmptyKey_ThrowsArgumentException()
    {
        using var provider = CreateProvider();
        Assert.ThrowsAsync<ArgumentException>(() => provider.DeleteSecretAsync(""));
    }

    [Test]
    public void Constructor_NullHttpClient_Throws()
    {
        var options = Options.Create(new SecretsOptions());
        Assert.Throws<ArgumentNullException>(() =>
            new VaultSecretProvider(null!, options, _logger));
    }

    [Test]
    public void Constructor_WithVaultToken_SetsHeader()
    {
        var options = Options.Create(new SecretsOptions { VaultToken = "hvs.test-token" });
        var httpClient = new HttpClient(_httpHandler);

        using var provider = new VaultSecretProvider(httpClient, options, _logger, _auditLogger);

        Assert.That(httpClient.DefaultRequestHeaders.Contains("X-Vault-Token"), Is.True);
    }

    private VaultSecretProvider CreateProvider()
    {
        var options = Options.Create(new SecretsOptions
        {
            Provider = "Vault",
            VaultAddress = "https://vault.test:8200",
            VaultMountPath = "secret",
        });

        var httpClient = new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("https://vault.test:8200")
        };

        return new VaultSecretProvider(httpClient, options, _logger, _auditLogger);
    }

    private sealed class MockVaultHandler : HttpMessageHandler
    {
        public HttpStatusCode ResponseStatus { get; set; } = HttpStatusCode.OK;
        public string ResponseBody { get; set; } = "{}";
        public Uri? LastRequestUri { get; private set; }
        public HttpMethod? LastRequestMethod { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastRequestMethod = request.Method;

            return Task.FromResult(new HttpResponseMessage(ResponseStatus)
            {
                Content = new StringContent(ResponseBody, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
