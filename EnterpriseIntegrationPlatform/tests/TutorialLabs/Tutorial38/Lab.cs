// ============================================================================
// Tutorial 38 – OpenTelemetry / Observability (Lab)
// ============================================================================
// EIP Pattern: Observability.
// E2E: Wire real InMemoryMessageStateStore and CorrelationPropagator with
// MockEndpoint to track message lifecycle events as envelopes flow.
// ============================================================================

using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial38;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _input = null!;
    private InMemoryMessageStateStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _input = new MockEndpoint("obs-in");
        _store = new InMemoryMessageStateStore();
    }

    [TearDown]
    public async Task TearDown() => await _input.DisposeAsync();

    [Test]
    public async Task RecordAndRetrieve_ByCorrelationId()
    {
        var correlationId = Guid.NewGuid();
        var evt = CreateEvent(correlationId, "Ingestion", DeliveryStatus.Pending);

        await _store.RecordAsync(evt);

        var results = await _store.GetByCorrelationIdAsync(correlationId);
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].CorrelationId, Is.EqualTo(correlationId));
        Assert.That(results[0].Stage, Is.EqualTo("Ingestion"));
    }

    [Test]
    public async Task RecordAndRetrieve_ByBusinessKey()
    {
        var evt = CreateEvent(Guid.NewGuid(), "Processing", DeliveryStatus.InFlight,
            businessKey: "ORD-123");

        await _store.RecordAsync(evt);

        var results = await _store.GetByBusinessKeyAsync("ORD-123");
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].BusinessKey, Is.EqualTo("ORD-123"));
    }

    [Test]
    public async Task GetLatestByCorrelationId_ReturnsNewestEvent()
    {
        var correlationId = Guid.NewGuid();

        await _store.RecordAsync(CreateEvent(correlationId, "Ingestion", DeliveryStatus.Pending));
        await _store.RecordAsync(CreateEvent(correlationId, "Routing", DeliveryStatus.InFlight));
        await _store.RecordAsync(CreateEvent(correlationId, "Delivery", DeliveryStatus.Delivered));

        var latest = await _store.GetLatestByCorrelationIdAsync(correlationId);
        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.Stage, Is.EqualTo("Delivery"));
        Assert.That(latest.Status, Is.EqualTo(DeliveryStatus.Delivered));
    }

    [Test]
    public async Task GetByMessageId_ReturnsMatchingEvents()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var evt = new MessageEvent
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            MessageType = "order.placed",
            Source = "Svc",
            Stage = "Ingestion",
            Status = DeliveryStatus.Pending,
        };

        await _store.RecordAsync(evt);

        var results = await _store.GetByMessageIdAsync(messageId);
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].MessageId, Is.EqualTo(messageId));
    }

    [Test]
    public void CorrelationPropagator_InjectTraceContext_ReturnsEnvelope()
    {
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "test.event");

        var enriched = CorrelationPropagator.InjectTraceContext(envelope);

        Assert.That(enriched, Is.Not.Null);
        Assert.That(enriched.MessageId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task MultipleStages_OrderedByRecordedAt()
    {
        var correlationId = Guid.NewGuid();
        var stages = new[] { "Ingestion", "Routing", "Transform", "Delivery" };

        foreach (var stage in stages)
            await _store.RecordAsync(CreateEvent(correlationId, stage, DeliveryStatus.InFlight));

        var trail = await _store.GetByCorrelationIdAsync(correlationId);
        Assert.That(trail, Has.Count.EqualTo(4));
        Assert.That(trail[0].Stage, Is.EqualTo("Ingestion"));
        Assert.That(trail[^1].Stage, Is.EqualTo("Delivery"));
    }

    [Test]
    public async Task E2E_MockEndpoint_RecordEventsAsEnvelopesFlow()
    {
        await _input.SubscribeAsync<string>("obs-topic", "obs-group",
            async envelope =>
            {
                await _store.RecordAsync(new MessageEvent
                {
                    MessageId = envelope.MessageId,
                    CorrelationId = envelope.CorrelationId,
                    MessageType = envelope.MessageType,
                    Source = envelope.Source,
                    Stage = "Ingestion",
                    Status = DeliveryStatus.Pending,
                });
            });

        var env1 = IntegrationEnvelope<string>.Create("order-1", "OrderSvc", "order.placed");
        var env2 = IntegrationEnvelope<string>.Create("order-2", "OrderSvc", "order.shipped");

        await _input.SendAsync(env1);
        await _input.SendAsync(env2);

        var trail1 = await _store.GetByCorrelationIdAsync(env1.CorrelationId);
        var trail2 = await _store.GetByCorrelationIdAsync(env2.CorrelationId);

        Assert.That(trail1, Has.Count.EqualTo(1));
        Assert.That(trail2, Has.Count.EqualTo(1));
        Assert.That(trail1[0].MessageType, Is.EqualTo("order.placed"));
        Assert.That(trail2[0].MessageType, Is.EqualTo("order.shipped"));
    }

    private static MessageEvent CreateEvent(
        Guid correlationId, string stage, DeliveryStatus status,
        string? businessKey = null) => new()
    {
        MessageId = Guid.NewGuid(),
        CorrelationId = correlationId,
        MessageType = "order.placed",
        Source = "OrderSvc",
        Stage = stage,
        Status = status,
        BusinessKey = businessKey,
    };
}
