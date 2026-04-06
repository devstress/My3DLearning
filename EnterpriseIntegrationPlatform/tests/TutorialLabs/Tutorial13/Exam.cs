// ============================================================================
// Tutorial 13 – Routing Slip (Exam)
// ============================================================================
// Coding challenges: build a multi-step processing pipeline, handle partial
// failure mid-slip, and verify step-by-step forwarding to destination topics.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial13;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Three-Step Pipeline — Validate → Transform → Deliver ───

    [Test]
    public async Task Challenge1_ThreeStepPipeline_ExecutesFirstStepAndAdvances()
    {
        // Build a routing slip with three steps: Validate → Transform → Deliver.
        // Execute the first step (Validate), verify it succeeds, and confirm
        // the remaining slip has two steps.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var validateHandler = Substitute.For<IRoutingSlipStepHandler>();
        validateHandler.StepName.Returns("Validate");
        validateHandler.HandleAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var transformHandler = Substitute.For<IRoutingSlipStepHandler>();
        transformHandler.StepName.Returns("Transform");

        var deliverHandler = Substitute.For<IRoutingSlipStepHandler>();
        deliverHandler.StepName.Returns("Deliver");

        var router = new RoutingSlipRouter(
            [validateHandler, transformHandler, deliverHandler],
            producer,
            NullLogger<RoutingSlipRouter>.Instance);

        var slip = new RoutingSlip([
            new RoutingSlipStep("Validate"),
            new RoutingSlipStep("Transform", "transform-topic"),
            new RoutingSlipStep("Deliver", "delivery-topic"),
        ]);

        var envelope = IntegrationEnvelope<string>.Create(
            "order-data", "OrderService", "order.created") with
        {
            Metadata = new Dictionary<string, string>
            {
                [RoutingSlip.MetadataKey] = JsonSerializer.Serialize(slip.Steps),
            },
        };

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.StepName, Is.EqualTo("Validate"));
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.RemainingSlip.Steps, Has.Count.EqualTo(2));
        Assert.That(result.RemainingSlip.CurrentStep!.StepName, Is.EqualTo("Transform"));
    }

    // ── Challenge 2: Mid-Pipeline Failure Halts Processing ──────────────────

    [Test]
    public async Task Challenge2_MidPipelineFailure_HaltsAtFailedStep()
    {
        // In a two-step slip (Validate → Enrich), if Validate fails, the
        // remaining slip should still contain both steps (no advancement).
        var producer = Substitute.For<IMessageBrokerProducer>();

        var validateHandler = Substitute.For<IRoutingSlipStepHandler>();
        validateHandler.StepName.Returns("Validate");
        validateHandler.HandleAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>())
            .Returns(false); // Validation fails.

        var enrichHandler = Substitute.For<IRoutingSlipStepHandler>();
        enrichHandler.StepName.Returns("Enrich");

        var router = new RoutingSlipRouter(
            [validateHandler, enrichHandler],
            producer,
            NullLogger<RoutingSlipRouter>.Instance);

        var slip = new RoutingSlip([
            new RoutingSlipStep("Validate"),
            new RoutingSlipStep("Enrich", "enrich-topic"),
        ]);

        var envelope = IntegrationEnvelope<string>.Create(
            "bad-data", "Service", "event.type") with
        {
            Metadata = new Dictionary<string, string>
            {
                [RoutingSlip.MetadataKey] = JsonSerializer.Serialize(slip.Steps),
            },
        };

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.StepName, Is.EqualTo("Validate"));
        // Slip was NOT advanced — both steps remain.
        Assert.That(result.RemainingSlip.Steps, Has.Count.EqualTo(2));
        Assert.That(result.ForwardedToTopic, Is.Null);

        // Producer was NOT called — no forwarding on failure.
        await producer.DidNotReceive().PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Challenge 3: Step with Destination Topic Forwards Message ────────────

    [Test]
    public async Task Challenge3_StepForwarding_PublishesToDestinationTopic()
    {
        // When a step has a DestinationTopic and succeeds, the router should
        // publish the envelope to that topic.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var handler = Substitute.For<IRoutingSlipStepHandler>();
        handler.StepName.Returns("Deliver");
        handler.HandleAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var router = new RoutingSlipRouter(
            [handler], producer, NullLogger<RoutingSlipRouter>.Instance);

        var slip = new RoutingSlip([
            new RoutingSlipStep("Deliver", "final-destination-topic"),
        ]);

        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "Service", "event.type") with
        {
            Metadata = new Dictionary<string, string>
            {
                [RoutingSlip.MetadataKey] = JsonSerializer.Serialize(slip.Steps),
            },
        };

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ForwardedToTopic, Is.EqualTo("final-destination-topic"));
        Assert.That(result.RemainingSlip.IsComplete, Is.True);

        // Verify the producer published to the correct topic.
        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("final-destination-topic"),
            Arg.Any<CancellationToken>());
    }
}
