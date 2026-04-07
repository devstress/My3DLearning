// ============================================================================
// Tutorial 19 – Content Filter (Lab)
// ============================================================================
// EIP Pattern: Content Filter.
// Real Integrations: ContentFilter keeping only specified JSON paths, verify
// filtered payload, and publish results via NatsBrokerEndpoint
// (real NATS JetStream via Aspire).
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial19;

[TestFixture]
public sealed class Lab
{
    // ── 1. Path-Based Filtering ──────────────────────────────────────

    [Test]
    public async Task Filter_RetainsSpecifiedPaths()
    {
        var filter = CreateFilter();

        var payload = """{"order":{"id":"ORD-1","total":99.99},"customer":{"name":"Alice","email":"a@b.com"},"internal":"secret"}""";
        var result = await filter.FilterAsync(payload, new[] { "order.id", "customer.name" });

        Assert.That(result, Does.Contain("ORD-1"));
        Assert.That(result, Does.Contain("Alice"));
        Assert.That(result, Does.Not.Contain("secret"));
        Assert.That(result, Does.Not.Contain("a@b.com"));
        Assert.That(result, Does.Not.Contain("99.99"));
    }

    [Test]
    public async Task Filter_MissingPath_SkippedSilently()
    {
        var filter = CreateFilter();

        var payload = """{"name":"Alice","age":30}""";
        var result = await filter.FilterAsync(payload, new[] { "name", "nonexistent" });

        Assert.That(result, Does.Contain("Alice"));
        Assert.That(result, Does.Not.Contain("30"));
    }

    [Test]
    public async Task Filter_NestedPaths_PreservesStructure()
    {
        var filter = CreateFilter();

        var payload = """{"address":{"city":"NYC","zip":"10001","street":"5th Ave"},"phone":"555-0123"}""";
        var result = await filter.FilterAsync(payload, new[] { "address.city", "address.zip" });

        Assert.That(result, Does.Contain("NYC"));
        Assert.That(result, Does.Contain("10001"));
        Assert.That(result, Does.Not.Contain("5th Ave"));
        Assert.That(result, Does.Not.Contain("555-0123"));
    }


    // ── 2. Validation & Error Handling ───────────────────────────────

    [Test]
    public void Filter_EmptyKeepPaths_ThrowsArgumentException()
    {
        var filter = CreateFilter();

        Assert.ThrowsAsync<ArgumentException>(
            () => filter.FilterAsync("""{"a":1}""", Array.Empty<string>()));
    }

    [Test]
    public void Filter_NonJsonObject_ThrowsInvalidOperation()
    {
        var filter = CreateFilter();

        Assert.ThrowsAsync<InvalidOperationException>(
            () => filter.FilterAsync("[1,2,3]", new[] { "a" }));
    }


    // ── 3. End-to-End Integration (Real NATS) ────────────────────────

    [Test]
    public async Task Filter_E2E_PublishFilteredToNatsEndpoint()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t19-e2e");
        var topic = AspireFixture.UniqueTopic("t19-filtered");

        var filter = CreateFilter();

        var payload = """{"order":{"id":"ORD-5","total":500,"status":"shipped"},"audit":{"user":"admin"}}""";
        var filtered = await filter.FilterAsync(payload, new[] { "order.id", "order.status" });

        var envelope = IntegrationEnvelope<string>.Create(
            filtered, "FilterService", "payload.filtered");
        await nats.PublishAsync(envelope, topic, CancellationToken.None);

        nats.AssertReceivedOnTopic(topic, 1);
        var received = nats.GetReceived<string>();
        Assert.That(received.Payload, Does.Contain("ORD-5"));
        Assert.That(received.Payload, Does.Contain("shipped"));
        Assert.That(received.Payload, Does.Not.Contain("admin"));
    }

    private static ContentFilter CreateFilter() =>
        new(NullLogger<ContentFilter>.Instance);
}
