// ============================================================================
// Tutorial 03 – Your First Message (Lab)
// ============================================================================
// EIP Patterns: Message, Message Channel (Point-to-Point, Publish-Subscribe)
// End-to-End: IntegrationEnvelope anatomy (auto-generated identity fields,
// causation chains, priority/intent/schema defaults), metadata propagation,
// message expiration, sequence numbers for split batches, and real channel
// components (PointToPointChannel, PublishSubscribeChannel) wired to
// MockEndpoint for verified delivery.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;

namespace TutorialLabs.Tutorial03;

public sealed record OrderPayload(string OrderId, string Product, int Quantity);

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("output");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    // ── 1. IntegrationEnvelope Anatomy ──────────────────────────────────

    [Test]
    public void Envelope_FactoryAutoGeneratesIdentityFields()
    {
        // IntegrationEnvelope.Create() generates unique MessageId,
        // CorrelationId, and a UTC Timestamp — the canonical identity
        // that follows the message through every processing step.
        var envelope = IntegrationEnvelope<string>.Create(
            "Hello, Messaging!", "Tutorial03", "greeting");

        Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(envelope.CorrelationId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(envelope.Timestamp, Is.GreaterThan(DateTimeOffset.MinValue));
        Assert.That(envelope.Source, Is.EqualTo("Tutorial03"));
        Assert.That(envelope.MessageType, Is.EqualTo("greeting"));
        Assert.That(envelope.Payload, Is.EqualTo("Hello, Messaging!"));

        // Each Create() call produces a distinct MessageId
        var envelope2 = IntegrationEnvelope<string>.Create(
            "Second", "Tutorial03", "greeting");
        Assert.That(envelope2.MessageId, Is.Not.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task Envelope_DomainObjectPayload_PreservedEndToEnd()
    {
        // A typed domain record survives the full publish → receive path.
        // The envelope preserves the payload type and all field values.
        var order = new OrderPayload("ORD-100", "Gadget", 3);
        var envelope = IntegrationEnvelope<OrderPayload>.Create(
            order, "OrderService", "order.created");

        await _output.PublishAsync(envelope, "orders");

        _output.AssertReceivedCount(1);
        var received = _output.GetReceived<OrderPayload>();
        Assert.That(received.Payload.OrderId, Is.EqualTo("ORD-100"));
        Assert.That(received.Payload.Product, Is.EqualTo("Gadget"));
        Assert.That(received.Payload.Quantity, Is.EqualTo(3));
        Assert.That(received.MessageId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public void Envelope_CausationId_LinksChildToParent()
    {
        // CausationId creates a parent→child lineage.
        // The child's CausationId = the parent's MessageId.
        // Both share the same CorrelationId for end-to-end tracing.
        var parent = IntegrationEnvelope<string>.Create(
            "original-order", "OrderService", "order.created");

        var child = IntegrationEnvelope<string>.Create(
            "order-validated", "ValidationService", "order.validated",
            correlationId: parent.CorrelationId,
            causationId: parent.MessageId);

        Assert.That(child.CausationId, Is.EqualTo(parent.MessageId));
        Assert.That(child.CorrelationId, Is.EqualTo(parent.CorrelationId));
        Assert.That(child.MessageId, Is.Not.EqualTo(parent.MessageId));
    }

    [Test]
    public void Envelope_PriorityIntentSchemaVersion_DefaultsAndOverrides()
    {
        // Defaults: Priority=Normal, SchemaVersion="1.0", Intent=null.
        // These can be overridden via init-only properties.
        var defaultEnvelope = IntegrationEnvelope<string>.Create(
            "payload", "svc", "type");

        Assert.That(defaultEnvelope.Priority, Is.EqualTo(MessagePriority.Normal));
        Assert.That(defaultEnvelope.SchemaVersion, Is.EqualTo("1.0"));
        Assert.That(defaultEnvelope.Intent, Is.Null);

        // Override using object initializer (with operator)
        var urgentCommand = IntegrationEnvelope<string>.Create(
            "process-now", "svc", "command.execute") with
        {
            Priority = MessagePriority.Critical,
            Intent = MessageIntent.Command,
            SchemaVersion = "2.0",
        };

        Assert.That(urgentCommand.Priority, Is.EqualTo(MessagePriority.Critical));
        Assert.That(urgentCommand.Intent, Is.EqualTo(MessageIntent.Command));
        Assert.That(urgentCommand.SchemaVersion, Is.EqualTo("2.0"));
    }

    // ── 2. Metadata & Message Lifecycle ─────────────────────────────────

    [Test]
    public async Task Envelope_Metadata_KeyValuePairsFlowWithMessage()
    {
        // Metadata dictionary carries arbitrary key-value pairs alongside
        // the message — used for headers, tracing, and custom context.
        var envelope = IntegrationEnvelope<string>.Create(
            "traced-payload", "svc", "event") with
        {
            Metadata = new Dictionary<string, string>
            {
                [MessageHeaders.TraceId] = "abc-trace-123",
                [MessageHeaders.ContentType] = "application/json",
                ["custom-header"] = "custom-value",
            },
        };

        await _output.PublishAsync(envelope, "events");

        var received = _output.GetReceived<string>();
        Assert.That(received.Metadata[MessageHeaders.TraceId], Is.EqualTo("abc-trace-123"));
        Assert.That(received.Metadata[MessageHeaders.ContentType], Is.EqualTo("application/json"));
        Assert.That(received.Metadata["custom-header"], Is.EqualTo("custom-value"));
    }

    [Test]
    public void Envelope_ExpiresAt_IsExpiredProperty()
    {
        // ExpiresAt + IsExpired implement the Message Expiration pattern.
        // Expired messages should be routed to the Dead Letter Queue.
        var futureEnvelope = IntegrationEnvelope<string>.Create(
            "not-expired", "svc", "type") with
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };
        Assert.That(futureEnvelope.IsExpired, Is.False);

        var pastEnvelope = IntegrationEnvelope<string>.Create(
            "already-expired", "svc", "type") with
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
        };
        Assert.That(pastEnvelope.IsExpired, Is.True);

        // No expiration = never expires
        var noExpiry = IntegrationEnvelope<string>.Create("eternal", "svc", "type");
        Assert.That(noExpiry.ExpiresAt, Is.Null);
        Assert.That(noExpiry.IsExpired, Is.False);
    }

    [Test]
    public async Task Envelope_SequenceNumbers_SplitBatchTracking()
    {
        // SequenceNumber + TotalCount track position within a split batch.
        // A Splitter produces N messages; each carries its index and total.
        var totalItems = 3;
        for (var i = 0; i < totalItems; i++)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"item-{i}", "Splitter", "batch.item") with
            {
                SequenceNumber = i,
                TotalCount = totalItems,
            };
            await _output.PublishAsync(envelope, "batch-items");
        }

        _output.AssertReceivedCount(3);
        var first = _output.GetReceived<string>(0);
        var last = _output.GetReceived<string>(2);
        Assert.That(first.SequenceNumber, Is.EqualTo(0));
        Assert.That(last.SequenceNumber, Is.EqualTo(2));
        Assert.That(first.TotalCount, Is.EqualTo(3));
    }

    // ── 3. Message Channels ─────────────────────────────────────────────

    [Test]
    public async Task PointToPointChannel_SendToQueue_SingleDelivery()
    {
        // Point-to-Point Channel: each message delivered to exactly one
        // consumer in the group — queue semantics.
        var channel = new PointToPointChannel(
            _output, _output, NullLogger<PointToPointChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-created", "OrderService", "order.created");

        await channel.SendAsync(envelope, "orders-queue", CancellationToken.None);

        _output.AssertReceivedCount(1);
        var received = _output.GetReceived<string>();
        Assert.That(received.Payload, Is.EqualTo("order-created"));
        Assert.That(received.MessageId, Is.EqualTo(envelope.MessageId));
    }

    [Test]
    public async Task PublishSubscribeChannel_FanOut_AllSubscribersReceive()
    {
        // Publish-Subscribe Channel: every subscriber receives every message
        // — fan-out delivery. Each subscriber gets its own consumer group.
        var sub1 = new MockEndpoint("subscriber-1");
        var sub2 = new MockEndpoint("subscriber-2");

        var channel1 = new PublishSubscribeChannel(
            sub1, sub1, NullLogger<PublishSubscribeChannel>.Instance);
        var channel2 = new PublishSubscribeChannel(
            sub2, sub2, NullLogger<PublishSubscribeChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "price-updated", "PricingService", "price.changed");

        // Same message published to both channels — simulates fan-out
        await channel1.PublishAsync(envelope, "price-events", CancellationToken.None);
        await channel2.PublishAsync(envelope, "price-events", CancellationToken.None);

        sub1.AssertReceivedCount(1);
        sub2.AssertReceivedCount(1);
        Assert.That(sub1.GetReceived<string>().Payload, Is.EqualTo("price-updated"));
        Assert.That(sub2.GetReceived<string>().Payload, Is.EqualTo("price-updated"));

        await sub1.DisposeAsync();
        await sub2.DisposeAsync();
    }

    [Test]
    public async Task TopicRouting_MessagesDeliveredToCorrectTopics()
    {
        // Different message types routed to different topics.
        // Each topic accumulates only its own messages.
        var channel = new PointToPointChannel(
            _output, _output, NullLogger<PointToPointChannel>.Instance);

        for (var i = 0; i < 3; i++)
        {
            var env = IntegrationEnvelope<string>.Create(
                $"order-{i}", "OrderService", "order.created");
            await channel.SendAsync(env, "orders", CancellationToken.None);
        }

        for (var i = 0; i < 2; i++)
        {
            var env = IntegrationEnvelope<string>.Create(
                $"payment-{i}", "PaymentService", "payment.processed");
            await channel.SendAsync(env, "payments", CancellationToken.None);
        }

        _output.AssertReceivedCount(5);
        _output.AssertReceivedOnTopic("orders", 3);
        _output.AssertReceivedOnTopic("payments", 2);
        Assert.That(_output.GetReceivedTopics(), Has.Count.EqualTo(2));
    }
}
