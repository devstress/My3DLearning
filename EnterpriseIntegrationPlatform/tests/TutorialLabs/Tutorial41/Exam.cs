// ============================================================================
// Tutorial 41 – OpenClaw Web / Blazor UI Concepts (Exam)
// ============================================================================
// EIP Pattern: Message State Tracking
// E2E: Full lifecycle recording, multi-message business-key correlation,
//      and message snapshot creation — all published through MockEndpoint.
// ============================================================================
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using EnterpriseIntegrationPlatform.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial41;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_FullMessageLifecycle_RecordAllStages_QueryAndPublish()
    {
        await using var output = new MockEndpoint("exam-lifecycle");
        var store = new InMemoryMessageStateStore();
        var corrId = Guid.NewGuid();

        var stages = new[]
        {
            ("Gateway", "Ingestion", DeliveryStatus.Pending),
            ("Router", "Routing", DeliveryStatus.InFlight),
            ("Transformer", "Transform", DeliveryStatus.InFlight),
            ("Connector", "Delivery", DeliveryStatus.Delivered),
        };

        foreach (var (source, stage, status) in stages)
        {
            await store.RecordAsync(new MessageEvent
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = corrId,
                MessageType = "OrderShipment",
                Source = source,
                Stage = stage,
                Status = status,
                BusinessKey = "ORD-42",
            });
        }

        var events = await store.GetByCorrelationIdAsync(corrId);
        Assert.That(events, Has.Count.EqualTo(4));

        var latest = await store.GetLatestByCorrelationIdAsync(corrId);
        Assert.That(latest!.Stage, Is.EqualTo("Delivery"));
        Assert.That(latest.Status, Is.EqualTo(DeliveryStatus.Delivered));

        foreach (var evt in events)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                $"{evt.Stage}:{evt.Status}", "state-store", "lifecycle.event");
            await output.PublishAsync(envelope, "audit-trail", default);
        }

        output.AssertReceivedOnTopic("audit-trail", 4);
    }

    [Test]
    public async Task Challenge2_MultipleMessagesSharedBusinessKey_QueryAndPublish()
    {
        await using var output = new MockEndpoint("exam-bizkey");
        var store = new InMemoryMessageStateStore();

        var corr1 = Guid.NewGuid();
        var corr2 = Guid.NewGuid();

        await store.RecordAsync(new MessageEvent
        {
            MessageId = Guid.NewGuid(), CorrelationId = corr1,
            MessageType = "Invoice", Source = "Billing", Stage = "Ingestion",
            Status = DeliveryStatus.Pending, BusinessKey = "INV-789",
        });
        await store.RecordAsync(new MessageEvent
        {
            MessageId = Guid.NewGuid(), CorrelationId = corr2,
            MessageType = "Invoice", Source = "Billing", Stage = "Delivery",
            Status = DeliveryStatus.Delivered, BusinessKey = "INV-789",
        });

        var events = await store.GetByBusinessKeyAsync("INV-789");
        Assert.That(events, Has.Count.EqualTo(2));

        foreach (var evt in events)
        {
            var envelope = IntegrationEnvelope<string>.Create(
                evt.Stage, "state-store", "bizkey.result");
            await output.PublishAsync(envelope, "bizkey-audit", default);
        }

        output.AssertReceivedOnTopic("bizkey-audit", 2);
    }

    [Test]
    public async Task Challenge3_MessageStateSnapshot_CreateAndPublish()
    {
        await using var output = new MockEndpoint("exam-snapshot");

        var log = new MockObservabilityEventLog();
        var traceAnalyzer = new MockTraceAnalyzer();
        var inspector = new MessageStateInspector(
            log, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "OrderService", "order.shipped");
        envelope.Metadata[MessageHeaders.TraceId] = "trace-abc-123";
        envelope.Metadata[MessageHeaders.RetryCount] = "2";

        var snapshot = inspector.CreateSnapshot(envelope, "Transform", DeliveryStatus.InFlight);
        Assert.That(snapshot.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(snapshot.CurrentStage, Is.EqualTo("Transform"));
        Assert.That(snapshot.RetryCount, Is.EqualTo(2));
        Assert.That(snapshot.TraceId, Is.EqualTo("trace-abc-123"));

        var result = IntegrationEnvelope<string>.Create(
            $"{snapshot.CurrentStage}:{snapshot.DeliveryStatus}", "inspector", "snapshot.created");
        await output.PublishAsync(result, "snapshots", default);
        output.AssertReceivedOnTopic("snapshots", 1);
    }
}
