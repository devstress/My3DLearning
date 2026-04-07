// ============================================================================
// InfrastructureConnectivityTests – Verifies Aspire test infrastructure works
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.InfrastructureTests;

/// <summary>
/// Smoke tests that verify the Aspire-hosted test infrastructure starts
/// and connects. Requires Docker; tests are skipped when unavailable.
/// </summary>
[TestFixture]
public sealed class InfrastructureConnectivityTests
{
    // ── NATS JetStream via Aspire ───────────────────────────────────────

    [Test]
    public async Task Nats_PublishAndReceive_RoundTrip()
    {
        var natsUrl = await SharedTestAppHost.GetNatsUrlAsync();
        if (natsUrl is null)
            Assert.Ignore("Docker not available — skipping NATS test");

        await using var endpoint = new NatsBrokerEndpoint("nats-test", natsUrl);

        var topic = $"test-{Guid.NewGuid():N}";
        var envelope = IntegrationEnvelope<string>.Create("Hello NATS!", "test", "greeting");

        // Subscribe first, then publish
        var received = new TaskCompletionSource<string>();
        await endpoint.SubscribeAsync<string>(topic, "test-group", env =>
        {
            received.TrySetResult(env.Payload);
            return Task.CompletedTask;
        });

        // Small delay to let subscription establish
        await Task.Delay(500);

        await endpoint.SendAsync(envelope, topic);

        // Wait for delivery with timeout
        var payload = await Task.WhenAny(received.Task, Task.Delay(10_000)) == received.Task
            ? received.Task.Result
            : null;

        Assert.That(payload, Is.EqualTo("Hello NATS!"),
            "Message should round-trip through real NATS JetStream");
    }

    [Test]
    public async Task Nats_ProducerCaptures_PublishedMessages()
    {
        var natsUrl = await SharedTestAppHost.GetNatsUrlAsync();
        if (natsUrl is null)
            Assert.Ignore("Docker not available — skipping NATS test");

        await using var endpoint = new NatsBrokerEndpoint("capture-test", natsUrl);

        var topic = $"capture-{Guid.NewGuid():N}";
        var envelope = IntegrationEnvelope<string>.Create("Captured!", "test", "cmd");

        await endpoint.PublishAsync(envelope, topic);

        endpoint.AssertReceivedCount(1);
        endpoint.AssertReceivedOnTopic(topic, 1);

        var msg = endpoint.GetReceived<string>();
        Assert.That(msg.Payload, Is.EqualTo("Captured!"));
    }

    // ── HTTP Test Server ────────────────────────────────────────────────

    [Test]
    public async Task HttpServer_ReceivesRequests_ReturnsConfiguredResponse()
    {
        await using var server = new TestHttpServer();
        server.WithJsonResponse("/api/test", new { status = "ok", value = 42 });
        await server.StartAsync();

        using var http = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };
        var response = await http.GetStringAsync("/api/test");

        Assert.That(response, Does.Contain("ok"));
        Assert.That(server.CallCount, Is.EqualTo(1));
        Assert.That(server.Calls[0].Path, Is.EqualTo("/api/test"));
    }

    [Test]
    public async Task HttpServer_DefaultResponse_MatchesUnknownPaths()
    {
        await using var server = new TestHttpServer();
        server.WithDefaultJsonResponse(new { fallback = true });
        await server.StartAsync();

        using var http = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };
        var response = await http.GetStringAsync("/unknown/path");

        Assert.That(response, Does.Contain("fallback"));
        Assert.That(server.CallCount, Is.EqualTo(1));
    }

    // ── Aspire TestAppHost Availability ──────────────────────────────────

    [Test]
    public async Task AspireTestAppHost_StartsSuccessfully()
    {
        var app = await SharedTestAppHost.GetAppAsync();
        if (app is null)
            Assert.Ignore("Docker not available — skipping Aspire test");

        Assert.That(SharedTestAppHost.IsAvailable, Is.True,
            "Aspire TestAppHost should be running");
    }

    // ── PostgreSQL via Aspire ───────────────────────────────────────────

    [Test]
    public async Task Postgres_PublishAndPoll_RoundTrip()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null)
            Assert.Ignore("Docker not available — skipping Postgres test");

        await using var endpoint = new PostgresBrokerEndpoint("pg-roundtrip-test", connStr);
        await endpoint.EnsureSchemaAsync();

        var topic = $"pg-test-{Guid.NewGuid():N}";
        var envelope = IntegrationEnvelope<string>.Create("Hello Postgres!", "test", "greeting");

        await endpoint.PublishAsync(envelope, topic);

        // Poll for the message
        var messages = await endpoint.PollAsync<string>(topic, "test-group", maxMessages: 10);

        Assert.That(messages.Count, Is.GreaterThanOrEqualTo(1),
            "At least one message should be retrieved from Postgres");
        Assert.That(messages[0].Payload, Is.EqualTo("Hello Postgres!"),
            "Message payload should match after Postgres round-trip");
    }

    [Test]
    public async Task Postgres_ProducerCaptures_PublishedMessages()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null)
            Assert.Ignore("Docker not available — skipping Postgres test");

        await using var endpoint = new PostgresBrokerEndpoint("pg-capture-test", connStr);
        await endpoint.EnsureSchemaAsync();

        var topic = $"pg-capture-{Guid.NewGuid():N}";
        var envelope = IntegrationEnvelope<string>.Create("PG Captured!", "test", "cmd");

        await endpoint.PublishAsync(envelope, topic);

        endpoint.AssertReceivedCount(1);
        endpoint.AssertReceivedOnTopic(topic, 1);

        var msg = endpoint.GetReceived<string>();
        Assert.That(msg.Payload, Is.EqualTo("PG Captured!"));
    }

    [Test]
    public async Task Postgres_SubscribeAndReceive_EventDriven()
    {
        var connStr = await SharedTestAppHost.GetPostgresConnectionStringAsync();
        if (connStr is null)
            Assert.Ignore("Docker not available — skipping Postgres test");

        await using var endpoint = new PostgresBrokerEndpoint("pg-subscribe-test", connStr);
        await endpoint.EnsureSchemaAsync();

        var topic = $"pg-sub-{Guid.NewGuid():N}";
        var received = new TaskCompletionSource<string>();

        // Subscribe first
        await endpoint.SubscribeAsync<string>(topic, "sub-group", env =>
        {
            received.TrySetResult(env.Payload);
            return Task.CompletedTask;
        });

        // Small delay for subscription to establish
        await Task.Delay(500);

        // Publish via a separate producer (simulating another service)
        await endpoint.SendAsync(
            IntegrationEnvelope<string>.Create("Event Driven!", "test", "event"),
            topic);

        // Wait for delivery (pg_notify + consumer loop polling at 200ms)
        var payload = await Task.WhenAny(received.Task, Task.Delay(15_000)) == received.Task
            ? received.Task.Result
            : null;

        Assert.That(payload, Is.EqualTo("Event Driven!"),
            "Message should round-trip through real PostgreSQL event-driven consumer");
    }
}
