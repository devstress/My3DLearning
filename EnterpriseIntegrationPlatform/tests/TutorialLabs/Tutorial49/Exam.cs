// ============================================================================
// Tutorial 49 – Testing Integrations (Exam)
// ============================================================================
// Coding challenges: message chain tracking, fault creation with exceptions,
// and routing slip lifecycle.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;

namespace TutorialLabs.Tutorial49;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Parent → Child Causation Chain ──────────────────────────

    [Test]
    public void Challenge1_CausationChain_ThreeGenerations()
    {
        var grandparent = IntegrationEnvelope<string>.Create(
            "gp-data", "ServiceA", "event.a");

        var parent = IntegrationEnvelope<string>.Create(
            "p-data", "ServiceB", "event.b",
            correlationId: grandparent.CorrelationId,
            causationId: grandparent.MessageId);

        var child = IntegrationEnvelope<string>.Create(
            "c-data", "ServiceC", "event.c",
            correlationId: grandparent.CorrelationId,
            causationId: parent.MessageId);

        // All share same correlation
        Assert.That(parent.CorrelationId, Is.EqualTo(grandparent.CorrelationId));
        Assert.That(child.CorrelationId, Is.EqualTo(grandparent.CorrelationId));

        // Causation chain: gp → p → c
        Assert.That(parent.CausationId, Is.EqualTo(grandparent.MessageId));
        Assert.That(child.CausationId, Is.EqualTo(parent.MessageId));

        // All unique MessageIds
        Assert.That(grandparent.MessageId, Is.Not.EqualTo(parent.MessageId));
        Assert.That(parent.MessageId, Is.Not.EqualTo(child.MessageId));
    }

    // ── Challenge 2: FaultEnvelope With Exception ───────────────────────────

    [Test]
    public void Challenge2_FaultEnvelope_WithException()
    {
        var original = IntegrationEnvelope<string>.Create(
            "{\"orderId\": \"ORD-1\"}", "OrderService", "order.created");

        var exception = new InvalidOperationException("Schema validation failed");

        var fault = FaultEnvelope.Create(
            original, "SchemaValidator", "Invalid payload", 2, exception);

        Assert.That(fault.FaultId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(fault.OriginalMessageId, Is.EqualTo(original.MessageId));
        Assert.That(fault.FaultedBy, Is.EqualTo("SchemaValidator"));
        Assert.That(fault.FaultReason, Is.EqualTo("Invalid payload"));
        Assert.That(fault.RetryCount, Is.EqualTo(2));
        Assert.That(fault.ErrorDetails, Does.Contain("Schema validation failed"));
    }

    // ── Challenge 3: Full Routing Slip Lifecycle ────────────────────────────

    [Test]
    public void Challenge3_RoutingSlipLifecycle_CreateAdvanceComplete()
    {
        var steps = new List<RoutingSlipStep>
        {
            new("validate", "t1"),
            new("enrich", "t2"),
            new("transform", "t3"),
            new("route", "t4"),
        };

        var slip = new RoutingSlip(steps);
        var visited = new List<string>();

        while (!slip.IsComplete)
        {
            visited.Add(slip.CurrentStep!.StepName);
            slip = slip.Advance();
        }

        Assert.That(visited, Is.EqualTo(new[] { "validate", "enrich", "transform", "route" }));
        Assert.That(slip.IsComplete, Is.True);
        Assert.That(slip.CurrentStep, Is.Null);
    }
}
