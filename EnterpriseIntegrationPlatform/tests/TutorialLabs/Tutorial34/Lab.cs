// ============================================================================
// Tutorial 34 – HTTP Connector (Lab)
// ============================================================================
// EIP Pattern: Connector
// E2E: HttpConnectorAdapter with NSubstitute IHttpConnector + MockEndpoint
//      for publishing send results.
// ============================================================================
using System.Text.Json;
using EnterpriseIntegrationPlatform.Connector.Http;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial34;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("http-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    private static HttpConnectorAdapter CreateAdapter(
        string name, IHttpConnector http, string baseUrl = "http://localhost") =>
        new(name, http,
            Options.Create(new HttpConnectorOptions { BaseUrl = baseUrl }),
            NullLogger<HttpConnectorAdapter>.Instance);

    [Test]
    public async Task Adapter_NameAndType_AreCorrect()
    {
        var http = Substitute.For<IHttpConnector>();
        var adapter = CreateAdapter("my-http", http);

        Assert.That(adapter.Name, Is.EqualTo("my-http"));
        Assert.That(adapter.ConnectorType, Is.EqualTo(ConnectorType.Http));
        await Task.CompletedTask;
    }

    [Test]
    public async Task SendAsync_Success_ReturnsOkResult()
    {
        var http = Substitute.For<IHttpConnector>();
        http.SendAsync<string, JsonElement>(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<string>(), Arg.Any<HttpMethod>(), Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("{}").RootElement);

        var adapter = CreateAdapter("test-http", http, "http://example.com");
        var envelope = IntegrationEnvelope<string>.Create("{\"key\":\"val\"}", "src", "Http.Send");
        var result = await adapter.SendAsync(
            envelope, new ConnectorSendOptions { Destination = "/api/data" });

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConnectorName, Is.EqualTo("test-http"));

        await _output.PublishAsync(envelope, "http-results", default);
        _output.AssertReceivedOnTopic("http-results", 1);
    }

    [Test]
    public async Task SendAsync_Failure_ReturnsFailResult()
    {
        var http = Substitute.For<IHttpConnector>();
        http.SendAsync<string, JsonElement>(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<string>(), Arg.Any<HttpMethod>(), Arg.Any<CancellationToken>())
            .Returns<JsonElement>(_ => throw new HttpRequestException("Connection refused"));

        var adapter = CreateAdapter("fail-http", http, "http://down.example.com");
        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "Http.Send");
        var result = await adapter.SendAsync(envelope, new ConnectorSendOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.Not.Null);
    }

    [Test]
    public async Task SendAsync_DefaultDestination_UsesSlash()
    {
        var http = Substitute.For<IHttpConnector>();
        http.SendAsync<string, JsonElement>(
            Arg.Any<IntegrationEnvelope<string>>(),
            "/", HttpMethod.Post, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("{}").RootElement);

        var adapter = CreateAdapter("default-dest", http);
        var envelope = IntegrationEnvelope<string>.Create("data", "src", "Send");
        var result = await adapter.SendAsync(envelope, new ConnectorSendOptions());

        Assert.That(result.Success, Is.True);
        await http.Received(1).SendAsync<string, JsonElement>(
            Arg.Any<IntegrationEnvelope<string>>(),
            "/", HttpMethod.Post, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TokenCache_SetAndRetrieve()
    {
        var cache = new InMemoryTokenCache();
        cache.SetToken("key1", "my-token", TimeSpan.FromMinutes(5));

        Assert.That(cache.TryGetToken("key1", out var token), Is.True);
        Assert.That(token, Is.EqualTo("my-token"));
        await Task.CompletedTask;
    }

    [Test]
    public async Task TokenCache_Expired_ReturnsFalse()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var cache = new InMemoryTokenCache(time);
        cache.SetToken("key2", "token-val", TimeSpan.FromSeconds(10));

        time.Advance(TimeSpan.FromSeconds(15));
        Assert.That(cache.TryGetToken("key2", out _), Is.False);
        await Task.CompletedTask;
    }

    [Test]
    public async Task HttpConnectorOptions_Defaults()
    {
        var opts = new HttpConnectorOptions();

        Assert.That(opts.TimeoutSeconds, Is.EqualTo(30));
        Assert.That(opts.MaxRetryAttempts, Is.EqualTo(3));
        Assert.That(opts.RetryDelayMs, Is.EqualTo(1000));
        Assert.That(opts.BaseUrl, Is.EqualTo(string.Empty));
        await Task.CompletedTask;
    }
}
