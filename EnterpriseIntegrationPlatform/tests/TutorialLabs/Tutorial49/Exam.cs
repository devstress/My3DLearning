// ============================================================================
// Tutorial 49 – Testing Integrations (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — causation chain_ three generations published
//   🔴 Advanced      — routing slip lifecycle_ publishes each step
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Contracts;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial49;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_CausationChain_ThreeGenerationsPublished()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t49-exam-chain");
        var topic = AspireFixture.UniqueTopic("t49-exam-events");
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic gp = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic parent = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic child = null!;

        // TODO: await nats.PublishAsync(...)
        // TODO: await nats.PublishAsync(...)
        // TODO: await nats.PublishAsync(...)

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
        // TODO: Create a RoutingSlip with appropriate configuration
        dynamic slip = null!;

        // TODO: Create a List with appropriate configuration
        dynamic visited = null!;
        while (!slip.IsComplete)
        {
            visited.Add(slip.CurrentStep!.StepName);
            // TODO: await nats.PublishAsync(...)
            slip = slip.Advance();
        }

        Assert.That(visited, Is.EqualTo(new[] { "validate", "enrich", "transform", "route" }));
        nats.AssertReceivedCount(4);
    }
}
#endif
