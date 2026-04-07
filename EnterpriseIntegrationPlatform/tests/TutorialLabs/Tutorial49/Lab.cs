// ============================================================================
// Tutorial 49 – Testing Integrations (Lab)
// ============================================================================
// EIP Pattern: Testing patterns for integration infrastructure.
// E2E: Demonstrate NatsBrokerEndpoint (real NATS JetStream via Aspire) usage for
// integration testing with real IntegrationEnvelope, FaultEnvelope, RoutingSlip.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial49;

[TestFixture]
public sealed class Lab
{
    // ── 1. NatsBrokerEndpoint Assertions ───────────────────────────────────

    [Test]
    public async Task NatsBrokerEndpoint_CapturesPublishedMessages()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t49-capture");
        var topic = AspireFixture.UniqueTopic("t49-capture");
        var envelope = IntegrationEnvelope<string>.Create("payload", "svc", "test.event");
        await nats.PublishAsync(envelope, topic, default);

        nats.AssertReceivedOnTopic(topic, 1);
        var received = nats.GetReceived<string>();
        Assert.That(received.Payload, Is.EqualTo("payload"));
    }

    [Test]
    public async Task NatsBrokerEndpoint_TracksMultipleTopics()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t49-multi");
        var topicA = AspireFixture.UniqueTopic("t49-topic-a");
        var topicB = AspireFixture.UniqueTopic("t49-topic-b");
        await nats.PublishAsync(
            IntegrationEnvelope<string>.Create("a", "svc", "type.a"), topicA, default);
        await nats.PublishAsync(
            IntegrationEnvelope<string>.Create("b", "svc", "type.b"), topicB, default);
        await nats.PublishAsync(
            IntegrationEnvelope<string>.Create("c", "svc", "type.a"), topicA, default);

        nats.AssertReceivedCount(3);
        nats.AssertReceivedOnTopic(topicA, 2);
        nats.AssertReceivedOnTopic(topicB, 1);
        Assert.That(nats.GetReceivedTopics(), Has.Count.EqualTo(2));
    }


    // ── 2. Envelope & Fault Contracts ────────────────────────────────

    [Test]
    public void IntegrationEnvelope_Create_SetsAllFields()
    {
        var envelope = IntegrationEnvelope<string>.Create("payload", "OrderService", "order.created");

        Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(envelope.CorrelationId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(envelope.Source, Is.EqualTo("OrderService"));
        Assert.That(envelope.MessageType, Is.EqualTo("order.created"));
        Assert.That(envelope.SchemaVersion, Is.EqualTo("1.0"));
    }

    [Test]
    public void CausationId_TracksDerivedMessages()
    {
        var parent = IntegrationEnvelope<string>.Create("parent", "ParentSvc", "parent.event");
        var child = IntegrationEnvelope<string>.Create(
            "child", "ChildSvc", "child.event",
            correlationId: parent.CorrelationId, causationId: parent.MessageId);

        Assert.That(child.CorrelationId, Is.EqualTo(parent.CorrelationId));
        Assert.That(child.CausationId, Is.EqualTo(parent.MessageId));
    }

    [Test]
    public void FaultEnvelope_CapturesOriginalDetails()
    {
        var original = IntegrationEnvelope<string>.Create("data", "OrderService", "order.created");
        var fault = FaultEnvelope.Create(original, "ValidationStep", "Invalid schema", 3);

        Assert.That(fault.OriginalMessageId, Is.EqualTo(original.MessageId));
        Assert.That(fault.FaultedBy, Is.EqualTo("ValidationStep"));
        Assert.That(fault.FaultReason, Is.EqualTo("Invalid schema"));
        Assert.That(fault.RetryCount, Is.EqualTo(3));
    }


    // ── 3. Routing Slip ──────────────────────────────────────────────

    [Test]
    public void RoutingSlip_Advance_MovesToNextStep()
    {
        var slip = new RoutingSlip(
        [
            new RoutingSlipStep("validate", "validate-topic"),
            new RoutingSlipStep("transform", "transform-topic"),
        ]);

        Assert.That(slip.CurrentStep!.StepName, Is.EqualTo("validate"));
        var next = slip.Advance();
        Assert.That(next.CurrentStep!.StepName, Is.EqualTo("transform"));
        var done = next.Advance();
        Assert.That(done.IsComplete, Is.True);
    }
}
