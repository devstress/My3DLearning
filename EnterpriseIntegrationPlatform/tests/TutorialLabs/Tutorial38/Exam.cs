// ============================================================================
// Tutorial 38 – OpenTelemetry (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — full lifecycle tracking_ through mock endpoint
//   🟡 Intermediate  — where is inspection_ with mocked services
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial38;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_FullLifecycleTracking_ThroughMockEndpoint()
    {
        await using var input = new MockEndpoint("exam-obs-in");
        // TODO: Create a InMemoryMessageStateStore with appropriate configuration
        dynamic store = null!;
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
            // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
            dynamic env = null!;
            env = env with { MessageId = messageId };
            // TODO: await input.SendAsync(...)
        }

        // TODO: var trail = await store.GetByCorrelationIdAsync(...)
        dynamic trail = null!;
        Assert.That(trail, Has.Count.EqualTo(4));
        Assert.That(trail[0].Stage, Is.EqualTo("Ingestion"));
        Assert.That(trail[^1].Stage, Is.EqualTo("Delivery"));
        Assert.That(trail[^1].Status, Is.EqualTo(DeliveryStatus.Delivered));

        // TODO: var latest = await store.GetLatestByCorrelationIdAsync(...)
        dynamic latest = null!;
        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.Stage, Is.EqualTo("Delivery"));
    }

    [Test]
    public async Task Intermediate_WhereIsInspection_WithMockedServices()
    {
        var correlationId = Guid.NewGuid();
        // TODO: Create a List with appropriate configuration
        dynamic events = null!;

        // TODO: Create a MockObservabilityEventLog with appropriate configuration
        dynamic eventLog = null!;

        // TODO: Create a MockTraceAnalyzer with appropriate configuration
        dynamic traceAnalyzer = null!;

        // TODO: Create a MessageStateInspector with appropriate configuration
        dynamic inspector = null!;

        // TODO: var result = await inspector.WhereIsAsync(...)
        dynamic result = null!;

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
#endif
