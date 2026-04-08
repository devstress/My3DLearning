// ============================================================================
// Tutorial 49 – Testing Integrations (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — causation chain_ three generations published
//   🔴 Advanced     — routing slip lifecycle_ publishes each step
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial49;

[TestFixture]
public sealed class ExamAnswers
{
    [Test]
    public async Task Starter_CausationChain_ThreeGenerationsPublished()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t49-exam-chain");
        var topic = AspireFixture.UniqueTopic("t49-exam-events");
        var gp = IntegrationEnvelope<string>.Create("gp", "SvcA", "event.a");
        var parent = IntegrationEnvelope<string>.Create(
            "p", "SvcB", "event.b", correlationId: gp.CorrelationId, causationId: gp.MessageId);
        var child = IntegrationEnvelope<string>.Create(
            "c", "SvcC", "event.c", correlationId: gp.CorrelationId, causationId: parent.MessageId);

        await nats.PublishAsync(gp, topic, default);
        await nats.PublishAsync(parent, topic, default);
        await nats.PublishAsync(child, topic, default);

        nats.AssertReceivedOnTopic(topic, 3);
        Assert.That(parent.CausationId, Is.EqualTo(gp.MessageId));
        Assert.That(child.CausationId, Is.EqualTo(parent.MessageId));
        Assert.That(child.CorrelationId, Is.EqualTo(gp.CorrelationId));
    }

    [Test]
    public void Intermediate_FaultEnvelope_WithException()
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
    public async Task Advanced_RoutingSlipLifecycle_PublishesEachStep()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t49-exam-slip");
        var topic = AspireFixture.UniqueTopic("t49-exam-steps");
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
            await nats.PublishAsync(
                IntegrationEnvelope<string>.Create(slip.CurrentStep.StepName, "test", "step.done"),
                topic, default);
            slip = slip.Advance();
        }

        Assert.That(visited, Is.EqualTo(new[] { "validate", "enrich", "transform", "route" }));
        nats.AssertReceivedCount(4);
    }
}
