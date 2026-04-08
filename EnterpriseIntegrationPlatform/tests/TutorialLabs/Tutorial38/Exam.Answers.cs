// ============================================================================
// Tutorial 38 – OpenTelemetry (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — full lifecycle tracking_ through mock endpoint
//   🟡 Intermediate — where is inspection_ with mocked services
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial38;

[TestFixture]
public sealed class ExamAnswers
{
    [Test]
    public async Task Starter_FullLifecycleTracking_ThroughMockEndpoint()
    {
        await using var input = new MockEndpoint("exam-obs-in");
        var store = new InMemoryMessageStateStore();
        var correlationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var stages = new[]
        {
            (Stage: "Ingestion", Status: DeliveryStatus.Pending),
            (Stage: "Routing",   Status: DeliveryStatus.InFlight),
            (Stage: "Transform", Status: DeliveryStatus.InFlight),
            (Stage: "Delivery",  Status: DeliveryStatus.Delivered),
        };

        // Subscribe handler that records lifecycle events
        int stageIndex = 0;
        await input.SubscribeAsync<string>("lifecycle-topic", "lifecycle-group",
            async envelope =>
            {
                if (stageIndex < stages.Length)
                {
                    var (stage, status) = stages[stageIndex++];
                    await store.RecordAsync(new MessageEvent
                    {
                        MessageId = messageId,
                        CorrelationId = correlationId,
                        MessageType = envelope.MessageType,
                        Source = envelope.Source,
                        Stage = stage,
                        Status = status,
                        BusinessKey = "ORD-999",
                    });
                }
            });

        // Feed envelope through each stage
        foreach (var _ in stages)
        {
            var env = IntegrationEnvelope<string>.Create(
                "order-data", "OrderSvc", "order.placed", correlationId);
            env = env with { MessageId = messageId };
            await input.SendAsync(env);
        }

        var trail = await store.GetByCorrelationIdAsync(correlationId);
        Assert.That(trail, Has.Count.EqualTo(4));
        Assert.That(trail[0].Stage, Is.EqualTo("Ingestion"));
        Assert.That(trail[^1].Stage, Is.EqualTo("Delivery"));
        Assert.That(trail[^1].Status, Is.EqualTo(DeliveryStatus.Delivered));

        var latest = await store.GetLatestByCorrelationIdAsync(correlationId);
        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.Stage, Is.EqualTo("Delivery"));
    }

    [Test]
    public async Task Intermediate_WhereIsInspection_WithMockedServices()
    {
        var correlationId = Guid.NewGuid();
        var events = new List<MessageEvent>
        {
            new()
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                MessageType = "order.placed",
                Source = "OrderSvc",
                Stage = "Routing",
                Status = DeliveryStatus.InFlight,
                BusinessKey = "ORD-555",
            },
        };

        var eventLog = new MockObservabilityEventLog().WithEvents(events.ToArray());

        var traceAnalyzer = new MockTraceAnalyzer()
            .WithWhereIsResponse("Message is currently being routed");

        var inspector = new MessageStateInspector(
            eventLog, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

        var result = await inspector.WhereIsAsync("ORD-555");

        Assert.That(result.Query, Is.EqualTo("ORD-555"));
        Assert.That(result.Found, Is.True);
        Assert.That(result.Events, Has.Count.EqualTo(1));
    }

    [Test]
    public void Advanced_CreateSnapshot_FromEnvelope()
    {
        var eventLog = new MockObservabilityEventLog();
        var traceAnalyzer = new MockTraceAnalyzer();
        var inspector = new MessageStateInspector(
            eventLog, traceAnalyzer, NullLogger<MessageStateInspector>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "Order data", "OrderSvc", "order.placed");

        var snapshot = inspector.CreateSnapshot(
            envelope, "Ingestion", DeliveryStatus.Pending);

        Assert.That(snapshot.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(snapshot.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(snapshot.MessageType, Is.EqualTo("order.placed"));
        Assert.That(snapshot.Source, Is.EqualTo("OrderSvc"));
        Assert.That(snapshot.CurrentStage, Is.EqualTo("Ingestion"));
        Assert.That(snapshot.DeliveryStatus, Is.EqualTo(DeliveryStatus.Pending));
    }
}
