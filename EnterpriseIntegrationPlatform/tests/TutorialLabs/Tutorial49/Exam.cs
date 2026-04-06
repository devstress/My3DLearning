// ============================================================================
// Tutorial 49 – Testing Integrations (Exam)
// ============================================================================
// E2E challenges: causation chain verification, fault envelope with exception,
// and full routing slip lifecycle via MockEndpoint.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial49;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_CausationChain_ThreeGenerationsPublished()
    {
        await using var output = new MockEndpoint("chain");
        var gp = IntegrationEnvelope<string>.Create("gp", "SvcA", "event.a");
        var parent = IntegrationEnvelope<string>.Create(
            "p", "SvcB", "event.b", correlationId: gp.CorrelationId, causationId: gp.MessageId);
        var child = IntegrationEnvelope<string>.Create(
            "c", "SvcC", "event.c", correlationId: gp.CorrelationId, causationId: parent.MessageId);

        await output.PublishAsync(gp, "events");
        await output.PublishAsync(parent, "events");
        await output.PublishAsync(child, "events");

        output.AssertReceivedOnTopic("events", 3);
        Assert.That(parent.CausationId, Is.EqualTo(gp.MessageId));
        Assert.That(child.CausationId, Is.EqualTo(parent.MessageId));
        Assert.That(child.CorrelationId, Is.EqualTo(gp.CorrelationId));
    }

    [Test]
    public void Challenge2_FaultEnvelope_WithException()
    {
        var original = IntegrationEnvelope<string>.Create(
            "{\"orderId\":\"ORD-1\"}", "OrderService", "order.created");
        var ex = new InvalidOperationException("Schema validation failed");
        var fault = FaultEnvelope.Create(original, "SchemaValidator", "Invalid payload", 2, ex);

        Assert.That(fault.OriginalMessageId, Is.EqualTo(original.MessageId));
        Assert.That(fault.FaultedBy, Is.EqualTo("SchemaValidator"));
        Assert.That(fault.RetryCount, Is.EqualTo(2));
        Assert.That(fault.ErrorDetails, Does.Contain("Schema validation failed"));
    }

    [Test]
    public async Task Challenge3_RoutingSlipLifecycle_PublishesEachStep()
    {
        await using var output = new MockEndpoint("slip");
        var slip = new RoutingSlip(
        [
            new RoutingSlipStep("validate", "t1"),
            new RoutingSlipStep("enrich", "t2"),
            new RoutingSlipStep("transform", "t3"),
            new RoutingSlipStep("route", "t4"),
        ]);

        var visited = new List<string>();
        while (!slip.IsComplete)
        {
            visited.Add(slip.CurrentStep!.StepName);
            await output.PublishAsync(
                IntegrationEnvelope<string>.Create(slip.CurrentStep.StepName, "test", "step.done"),
                "step-events");
            slip = slip.Advance();
        }

        Assert.That(visited, Is.EqualTo(new[] { "validate", "enrich", "transform", "route" }));
        output.AssertReceivedCount(4);
    }
}
