// ============================================================================
// Tutorial 04 – Integration Envelope (Lab)
// ============================================================================
// EIP Pattern: Envelope Wrapper
// End-to-End: Create envelopes with all fields (expiration, sequence,
// metadata, priority, causation chain), route through pipeline, verify
// wrapper fields preserved.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;

namespace TutorialLabs.Tutorial04;

public sealed record ShipmentPayload(
    string ShipmentId, string Carrier, decimal WeightKg, string[] Items);

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp()
    {
        _output = new MockEndpoint("output");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _output.DisposeAsync();
    }

    [Test]
    public async Task EndToEnd_ExpiresAt_PreservedThroughPipeline()
    {
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        var envelope = IntegrationEnvelope<string>.Create(
            "expiring", "source", "type") with { ExpiresAt = expiry };

        await _output.PublishAsync(envelope, "topic");

        var received = _output.GetReceived<string>();
        Assert.That(received.ExpiresAt, Is.EqualTo(expiry));
        Assert.That(received.IsExpired, Is.False);
    }

    [Test]
    public async Task EndToEnd_SequenceNumbers_PreservedThroughPipeline()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "part-2", "Splitter", "order.part") with
        {
            SequenceNumber = 2,
            TotalCount = 5,
        };

        await _output.PublishAsync(envelope, "parts");

        var received = _output.GetReceived<string>();
        Assert.That(received.SequenceNumber, Is.EqualTo(2));
        Assert.That(received.TotalCount, Is.EqualTo(5));
    }

    [Test]
    public async Task EndToEnd_MetadataHeaders_PreservedThroughPipeline()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "source", "type") with
        {
            Metadata = new Dictionary<string, string>
            {
                [MessageHeaders.ContentType] = "application/json",
                [MessageHeaders.TraceId] = "abc-123",
            },
        };

        await _output.PublishAsync(envelope, "topic");

        var received = _output.GetReceived<string>();
        Assert.That(received.Metadata[MessageHeaders.ContentType],
            Is.EqualTo("application/json"));
        Assert.That(received.Metadata[MessageHeaders.TraceId],
            Is.EqualTo("abc-123"));
    }

    [Test]
    public async Task EndToEnd_CriticalPriority_PreservedThroughPipeline()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "urgent", "AlertService", "alert") with
        {
            Priority = MessagePriority.Critical,
        };

        await _output.PublishAsync(envelope, "alerts");

        var received = _output.GetReceived<string>();
        Assert.That(received.Priority, Is.EqualTo(MessagePriority.Critical));
    }

    [Test]
    public async Task EndToEnd_CausationChain_PreservedThroughPipeline()
    {
        var parentId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var envelope = IntegrationEnvelope<string>.Create(
            "child", "ChildService", "child.created",
            correlationId: correlationId,
            causationId: parentId);

        await _output.PublishAsync(envelope, "topic");

        var received = _output.GetReceived<string>();
        Assert.That(received.CausationId, Is.EqualTo(parentId));
        Assert.That(received.CorrelationId, Is.EqualTo(correlationId));
    }

    [Test]
    public async Task EndToEnd_ReplyTo_PreservedThroughPipeline()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "request", "Requester", "req") with
        {
            ReplyTo = "reply-channel",
        };

        await _output.PublishAsync(envelope, "requests");

        var received = _output.GetReceived<string>();
        Assert.That(received.ReplyTo, Is.EqualTo("reply-channel"));
    }

    [Test]
    public async Task EndToEnd_AllWrapperFields_PreservedThroughPipeline()
    {
        var shipment = new ShipmentPayload("SHIP-1", "FedEx", 12.5m,
            new[] { "SKU-001", "SKU-002" });
        var correlationId = Guid.NewGuid();

        var envelope = IntegrationEnvelope<ShipmentPayload>.Create(
            shipment, "Warehouse", "shipment.dispatched",
            correlationId: correlationId) with
        {
            SchemaVersion = "2.0",
            Priority = MessagePriority.High,
            Intent = MessageIntent.Event,
            ReplyTo = "shipment-replies",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            SequenceNumber = 0,
            TotalCount = 3,
            Metadata = new Dictionary<string, string>
            {
                [MessageHeaders.ContentType] = "application/json",
            },
        };

        await _output.PublishAsync(envelope, "shipments");

        _output.AssertReceivedCount(1);
        var received = _output.GetReceived<ShipmentPayload>();
        Assert.That(received.Payload.ShipmentId, Is.EqualTo("SHIP-1"));
        Assert.That(received.SchemaVersion, Is.EqualTo("2.0"));
        Assert.That(received.Priority, Is.EqualTo(MessagePriority.High));
        Assert.That(received.Intent, Is.EqualTo(MessageIntent.Event));
        Assert.That(received.ReplyTo, Is.EqualTo("shipment-replies"));
        Assert.That(received.SequenceNumber, Is.EqualTo(0));
        Assert.That(received.TotalCount, Is.EqualTo(3));
    }
}
