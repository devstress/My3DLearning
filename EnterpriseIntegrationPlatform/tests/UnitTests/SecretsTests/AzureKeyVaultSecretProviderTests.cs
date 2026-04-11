using System.Net;
using System.Text.Json;
using EnterpriseIntegrationPlatform.Security.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit.SecretsTests;

[TestFixture]
public sealed class AzureKeyVaultSecretProviderTests
{
    private MockHttpHandler _httpHandler = null!;
    private SecretAuditLogger _auditLogger = null!;
    private ILogger<AzureKeyVaultSecretProvider> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _httpHandler = new MockHttpHandler();
        _auditLogger = new SecretAuditLogger(Substitute.For<ILogger<SecretAuditLogger>>());
        _logger = Substitute.For<ILogger<AzureKeyVaultSecretProvider>>();
    }

    [TearDown]
    public void TearDown()
    {
        _httpHandler.Dispose();
    }

    [Test]
    public async Task GetSecretAsync_Success_ReturnsSecretEntry()
    {
        var json = JsonSerializer.Serialize(new
        {
            value = "my-secret",
            id = "https://myvault.vault.azure.net/secrets/db-password/v1",
            attributes = new { created = 1700000000L, exp = 1800000000L },
            tags = new Dictionary<string, string> { ["env"] = "prod" }
        });

        _httpHandler.ResponseBody = json;
        _httpHandler.ResponseStatus = HttpStatusCode.OK;

        using var provider = CreateProvider();
        var result = await provider.GetSecretAsync("db-password");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Key, Is.EqualTo("db-password"));
        Assert.That(result.Value, Is.EqualTo("my-secret"));
        Assert.That(result.Version, Is.EqualTo("v1"));
        Assert.That(result.Metadata, Is.Not.Null);
        Assert.That(result.Metadata!["env"], Is.EqualTo("prod"));
    }

    [Test]
    public async Task GetSecretAsync_NotFound_ReturnsNull()
    {
        _httpHandler.ResponseStatus = HttpStatusCode.NotFound;
        _httpHandler.ResponseBody = "{}";

        using var provider = CreateProvider();
        var result = await provider.GetSecretAsync("missing-key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetSecretAsync_WithVersion_IncludesVersionInPath()
    {
        var json = JsonSerializer.Serialize(new
        {
            value = "old-value",
            id = "https://myvault.vault.azure.net/secrets/key/ver2"
        });

        _httpHandler.ResponseBody = json;
        _httpHandler.ResponseStatus = HttpStatusCode.OK;

        using var provider = CreateProvider();
        var result = await provider.GetSecretAsync("key", "ver2");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Version, Is.EqualTo("ver2"));
        Assert.That(_httpHandler.LastRequestUri!.ToString(), Does.Contain("/ver2"));
    }

    [Test]
    public async Task GetSecretAsync_NullBody_ReturnsNull()
    {
        _httpHandler.ResponseBody = "null";
        _httpHandler.ResponseStatus = HttpStatusCode.OK;

        using var provider = CreateProvider();
        var result = await provider.GetSecretAsync("key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SetSecretAsync_Success_ReturnsEntryWithVersion()
    {
        var json = JsonSerializer.Serialize(new
        {
            value = "new-secret",
            id = "https://myvault.vault.azure.net/secrets/api-key/v3"
        });

        _httpHandler.ResponseBody = json;
        _httpHandler.ResponseStatus = HttpStatusCode.OK;

        using var provider = CreateProvider();
        var result = await provider.SetSecretAsync("api-key", "new-secret");

        Assert.That(result.Key, Is.EqualTo("api-key"));
        Assert.That(result.Value, Is.EqualTo("new-secret"));
        Assert.That(result.Version, Is.EqualTo("v3"));
    }

    [Test]
    public async Task SetSecretAsync_WithMetadata_IncludesMetadataInRequest()
    {
        var json = JsonSerializer.Serialize(new
        {
            value = "val",
            id = "https://myvault.vault.azure.net/secrets/key/v1"
        });

        _httpHandler.ResponseBody = json;
        _httpHandler.ResponseStatus = HttpStatusCode.OK;

        var metadata = new Dictionary<string, string> { ["env"] = "staging" };

        using var provider = CreateProvider();
        var result = await provider.SetSecretAsync("key", "val", metadata);

        Assert.That(result, Is.Not.Null);
        Assert.That(_httpHandler.LastRequestMethod, Is.EqualTo(HttpMethod.Put));
    }

    [Test]
    public async Task DeleteSecretAsync_Success_ReturnsTrue()
    {
        _httpHandler.ResponseBody = "{}";
        _httpHandler.ResponseStatus = HttpStatusCode.OK;

        using var provider = CreateProvider();
        var result = await provider.DeleteSecretAsync("old-key");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeleteSecretAsync_NotFound_ReturnsFalse()
    {
        _httpHandler.ResponseBody = "{}";
        _httpHandler.ResponseStatus = HttpStatusCode.NotFound;

        using var provider = CreateProvider();
        var result = await provider.DeleteSecretAsync("missing");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ListSecretKeysAsync_ReturnsKeys()
    {
        var json = JsonSerializer.Serialize(new
        {
            value = new[]
            {
                new { id = "https://myvault.vault.azure.net/secrets/key1" },
                new { id = "https://myvault.vault.azure.net/secrets/key2" }
            }
        });

        _httpHandler.ResponseBody = json;
        _httpHandler.ResponseStatus = HttpStatusCode.OK;

        using var provider = CreateProvider();
        var keys = await provider.ListSecretKeysAsync();

        Assert.That(keys, Has.Count.EqualTo(2));
        Assert.That(keys, Does.Contain("key1"));
        Assert.That(keys, Does.Contain("key2"));
    }

    [Test]
    public async Task ListSecretKeysAsync_WithPrefix_FiltersKeys()
    {
        var json = JsonSerializer.Serialize(new
        {
            value = new[]
            {
                new { id = "https://myvault.vault.azure.net/secrets/db-password" },
                new { id = "https://myvault.vault.azure.net/secrets/db-user" },
                new { id = "https://myvault.vault.azure.net/secrets/api-key" }
            }
        });

        _httpHandler.ResponseBody = json;
        _httpHandler.ResponseStatus = HttpStatusCode.OK;

        using var provider = CreateProvider();
        var keys = await provider.ListSecretKeysAsync("db-");

        Assert.That(keys, Has.Count.EqualTo(2));
        Assert.That(keys, Has.All.StartsWith("db-"));
    }

    [Test]
    public async Task ListSecretKeysAsync_Failure_ReturnsEmpty()
    {
        _httpHandler.ResponseBody = "{}";
        _httpHandler.ResponseStatus = HttpStatusCode.InternalServerError;

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
    public void GetSecretAsync_EmptyKey_ThrowsArgumentException()
    {
        using var provider = CreateProvider();
        Assert.ThrowsAsync<ArgumentException>(() => provider.GetSecretAsync(""));
    }

    [Test]
    public void SetSecretAsync_NullKey_ThrowsArgumentException()
    {
        using var provider = CreateProvider();
        Assert.ThrowsAsync<ArgumentNullException>(() => provider.SetSecretAsync(null!, "value"));
    }

    [Test]
    public void SetSecretAsync_NullValue_ThrowsArgumentNullException()
    {
        using var provider = CreateProvider();
        Assert.ThrowsAsync<ArgumentNullException>(() => provider.SetSecretAsync("key", null!));
    }

    [Test]
    public void Constructor_NullHttpClient_Throws()
    {
        var options = Options.Create(new SecretsOptions());
        Assert.Throws<ArgumentNullException>(() =>
            new AzureKeyVaultSecretProvider(null!, options, _logger));
    }

    [Test]
    public void Constructor_NullOptions_Throws()
    {
        var httpClient = new HttpClient(_httpHandler);
        Assert.Throws<ArgumentNullException>(() =>
            new AzureKeyVaultSecretProvider(httpClient, null!, _logger));
    }

    private AzureKeyVaultSecretProvider CreateProvider()
    {
        var options = Options.Create(new SecretsOptions
        {
            Provider = "AzureKeyVault",
            AzureKeyVaultUri = "https://myvault.vault.azure.net",
        });

        var httpClient = new HttpClient(_httpHandler) { BaseAddress = new Uri("https://myvault.vault.azure.net") };
        return new AzureKeyVaultSecretProvider(httpClient, options, _logger, _auditLogger);
    }

    /// <summary>
    /// Minimal HTTP handler mock that returns configurable responses.
    /// </summary>
    private sealed class MockHttpHandler : HttpMessageHandler
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
