// ============================================================================
// Tutorial 34 – HTTP Connector (Exam)
// ============================================================================
// EIP Pattern: Connector
// E2E: HttpConnectorAdapter send with custom destination, token caching
//      lifecycle, and multiple independent connectors with MockEndpoint.
// ============================================================================
using System.Text.Json;
using EnterpriseIntegrationPlatform.Connector.Http;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial34;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_SendToCustomDestination_PublishesResult()
    {
        await using var output = new MockEndpoint("exam-http-dest");
        var http = new MockHttpConnector()
            .WithResponse<JsonElement>("/api/orders", JsonDocument.Parse("{\"id\":1}").RootElement);

        var adapter = new HttpConnectorAdapter(
            "order-http", http,
            Options.Create(new HttpConnectorOptions { BaseUrl = "http://orders.api" }),
            NullLogger<HttpConnectorAdapter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "{\"item\":\"widget\"}", "shop", "Order.Create");
        var result = await adapter.SendAsync(
            envelope, new ConnectorSendOptions { Destination = "/api/orders" });

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConnectorName, Is.EqualTo("order-http"));

        await output.PublishAsync(envelope, "order-results", default);
        output.AssertReceivedOnTopic("order-results", 1);
    }

    [Test]
    public async Task Challenge2_TokenCachingLifecycle()
    {
        await using var output = new MockEndpoint("exam-token");
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var cache = new InMemoryTokenCache(time);

        cache.SetToken("auth-endpoint", "token-abc", TimeSpan.FromSeconds(30));
        Assert.That(cache.TryGetToken("auth-endpoint", out var t1), Is.True);
        Assert.That(t1, Is.EqualTo("token-abc"));

        time.Advance(TimeSpan.FromSeconds(31));
        Assert.That(cache.TryGetToken("auth-endpoint", out _), Is.False);

        cache.SetToken("auth-endpoint", "token-xyz", TimeSpan.FromSeconds(60));
        Assert.That(cache.TryGetToken("auth-endpoint", out var t2), Is.True);
        Assert.That(t2, Is.EqualTo("token-xyz"));

        var envelope = IntegrationEnvelope<string>.Create(
            "token-lifecycle-ok", "cache", "TokenStatus");
        await output.PublishAsync(envelope, "token-events", default);
        output.AssertReceivedOnTopic("token-events", 1);
    }

    [Test]
    public async Task Challenge3_MultipleConnectors_IndependentResults()
    {
        await using var output = new MockEndpoint("exam-multi");
        var connectors = new[] { "api-a", "api-b" };

        foreach (var name in connectors)
        {
            var http = new MockHttpConnector();

            var adapter = new HttpConnectorAdapter(
                name, http,
                Options.Create(new HttpConnectorOptions { BaseUrl = $"http://{name}.local" }),
                NullLogger<HttpConnectorAdapter>.Instance);

            var envelope = IntegrationEnvelope<string>.Create($"msg-{name}", "test", "Send");
            var result = await adapter.SendAsync(envelope, new ConnectorSendOptions());
            Assert.That(result.Success, Is.True);

            await output.PublishAsync(envelope, $"results.{name}", default);
        }

        output.AssertReceivedCount(2);
        output.AssertReceivedOnTopic("results.api-a", 1);
        output.AssertReceivedOnTopic("results.api-b", 1);
    }
}
