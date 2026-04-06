// ============================================================================
// Tutorial 49 – Testing Integrations (Lab)
// ============================================================================
// This lab exercises testing patterns with IntegrationEnvelope, FaultEnvelope,
// RoutingSlip, message enums, and the IMessagingMapper contract.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;

namespace TutorialLabs.Tutorial49;

[TestFixture]
public sealed class Lab
{
    // ── IntegrationEnvelope.Create Sets All Fields ───────────────────────────

    [Test]
    public void IntegrationEnvelope_Create_SetsAllMandatoryFields()
    {
        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "OrderService", "order.created");

        Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(envelope.CorrelationId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(envelope.Source, Is.EqualTo("OrderService"));
        Assert.That(envelope.MessageType, Is.EqualTo("order.created"));
        Assert.That(envelope.Payload, Is.EqualTo("payload"));
        Assert.That(envelope.SchemaVersion, Is.EqualTo("1.0"));
    }

    // ── CausationId Chain ───────────────────────────────────────────────────

    [Test]
    public void IntegrationEnvelope_CausationId_TracksDerivedMessages()
    {
        var parent = IntegrationEnvelope<string>.Create(
            "parent-data", "ParentService", "parent.event");

        var child = IntegrationEnvelope<string>.Create(
            "child-data", "ChildService", "child.event",
            correlationId: parent.CorrelationId,
            causationId: parent.MessageId);

        Assert.That(child.CorrelationId, Is.EqualTo(parent.CorrelationId));
        Assert.That(child.CausationId, Is.EqualTo(parent.MessageId));
    }

    // ── FaultEnvelope.Create Captures Details ───────────────────────────────

    [Test]
    public void FaultEnvelope_Create_CapturesOriginalMessageDetails()
    {
        var original = IntegrationEnvelope<string>.Create(
            "data", "OrderService", "order.created");

        var fault = FaultEnvelope.Create(
            original, "ValidationStep", "Invalid schema", 3);

        Assert.That(fault.OriginalMessageId, Is.EqualTo(original.MessageId));
        Assert.That(fault.CorrelationId, Is.EqualTo(original.CorrelationId));
        Assert.That(fault.FaultedBy, Is.EqualTo("ValidationStep"));
        Assert.That(fault.FaultReason, Is.EqualTo("Invalid schema"));
        Assert.That(fault.RetryCount, Is.EqualTo(3));
    }

    // ── MessagePriority Enum Values ─────────────────────────────────────────

    [Test]
    public void MessagePriority_EnumValues()
    {
        Assert.That(Enum.GetValues<MessagePriority>(), Has.Length.GreaterThanOrEqualTo(4));
        Assert.That((int)MessagePriority.Low, Is.EqualTo(0));
        Assert.That((int)MessagePriority.Normal, Is.EqualTo(1));
        Assert.That((int)MessagePriority.High, Is.EqualTo(2));
        Assert.That((int)MessagePriority.Critical, Is.EqualTo(3));
    }

    // ── MessageIntent Enum Values ───────────────────────────────────────────

    [Test]
    public void MessageIntent_EnumValues()
    {
        Assert.That(Enum.GetValues<MessageIntent>(), Has.Length.GreaterThanOrEqualTo(3));
        Assert.That((int)MessageIntent.Command, Is.EqualTo(0));
        Assert.That((int)MessageIntent.Document, Is.EqualTo(1));
        Assert.That((int)MessageIntent.Event, Is.EqualTo(2));
    }

    // ── RoutingSlip Advance ─────────────────────────────────────────────────

    [Test]
    public void RoutingSlip_Advance_MovesToNextStep()
    {
        var slip = new RoutingSlip
        {
            Steps =
            [
                new RoutingSlipStep { StepName = "validate", DestinationTopic = "validate-topic" },
                new RoutingSlipStep { StepName = "transform", DestinationTopic = "transform-topic" },
                new RoutingSlipStep { StepName = "route", DestinationTopic = "route-topic" },
            ],
        };

        Assert.That(slip.CurrentStep!.StepName, Is.EqualTo("validate"));
        Assert.That(slip.IsComplete, Is.False);

        var next = slip.Advance();
        Assert.That(next.CurrentStep!.StepName, Is.EqualTo("transform"));

        var last = next.Advance();
        Assert.That(last.CurrentStep!.StepName, Is.EqualTo("route"));

        var done = last.Advance();
        Assert.That(done.IsComplete, Is.True);
    }

    // ── RoutingSlip IsComplete ──────────────────────────────────────────────

    [Test]
    public void RoutingSlip_EmptySteps_IsComplete()
    {
        var slip = new RoutingSlip { Steps = [] };

        Assert.That(slip.IsComplete, Is.True);
        Assert.That(slip.CurrentStep, Is.Null);
    }
}
