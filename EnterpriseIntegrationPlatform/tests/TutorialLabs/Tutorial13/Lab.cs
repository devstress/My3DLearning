// ============================================================================
// Tutorial 13 – Routing Slip (Lab)
// ============================================================================
// This lab exercises the RoutingSlipRouter — a pattern where each message
// carries its own processing itinerary. Steps are executed sequentially;
// after each step the slip is advanced and the message may be forwarded
// to a destination topic.
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
public sealed class Lab
{
    // ── Execute a Single Step Successfully ───────────────────────────────────

    [Test]
    public async Task Execute_SingleStep_SucceedsAndAdvancesSlip()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        // Create a handler that always succeeds.
        var handler = Substitute.For<IRoutingSlipStepHandler>();
        handler.StepName.Returns("Validate");
        handler.HandleAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var router = new RoutingSlipRouter(
            [handler], producer, NullLogger<RoutingSlipRouter>.Instance);

        // Build an envelope with a routing slip in metadata.
        var slip = new RoutingSlip([new RoutingSlipStep("Validate", "output-topic")]);
        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "Service", "event.type") with
        {
            Metadata = new Dictionary<string, string>
            {
                [RoutingSlip.MetadataKey] = JsonSerializer.Serialize(slip.Steps),
            },
        };

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.StepName, Is.EqualTo("Validate"));
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.FailureReason, Is.Null);
        Assert.That(result.RemainingSlip.IsComplete, Is.True);
        Assert.That(result.ForwardedToTopic, Is.EqualTo("output-topic"));
    }

    // ── Step Fails — Handler Returns False ──────────────────────────────────

    [Test]
    public async Task Execute_StepFails_ResultIndicatesFailure()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var handler = Substitute.For<IRoutingSlipStepHandler>();
        handler.StepName.Returns("Validate");
        handler.HandleAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        var router = new RoutingSlipRouter(
            [handler], producer, NullLogger<RoutingSlipRouter>.Instance);

        var slip = new RoutingSlip([new RoutingSlipStep("Validate", "output-topic")]);
        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "Service", "event.type") with
        {
            Metadata = new Dictionary<string, string>
            {
                [RoutingSlip.MetadataKey] = JsonSerializer.Serialize(slip.Steps),
            },
        };

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Is.Not.Null);
        Assert.That(result.ForwardedToTopic, Is.Null);
    }

    // ── No Handler Registered — Step Fails ──────────────────────────────────

    [Test]
    public async Task Execute_NoHandlerForStep_FailsWithReason()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        // Register a handler for "Transform" but the slip calls "Validate".
        var handler = Substitute.For<IRoutingSlipStepHandler>();
        handler.StepName.Returns("Transform");

        var router = new RoutingSlipRouter(
            [handler], producer, NullLogger<RoutingSlipRouter>.Instance);

        var slip = new RoutingSlip([new RoutingSlipStep("Validate")]);
        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "Service", "event.type") with
        {
            Metadata = new Dictionary<string, string>
            {
                [RoutingSlip.MetadataKey] = JsonSerializer.Serialize(slip.Steps),
            },
        };

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Does.Contain("Validate"));
    }

    // ── Multi-Step Slip — Advance Through Steps ─────────────────────────────

    [Test]
    public async Task Execute_MultiStepSlip_AdvancesToNextStep()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var validateHandler = Substitute.For<IRoutingSlipStepHandler>();
        validateHandler.StepName.Returns("Validate");
        validateHandler.HandleAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var router = new RoutingSlipRouter(
            [validateHandler], producer, NullLogger<RoutingSlipRouter>.Instance);

        // Slip with two steps: Validate (no forwarding) → Transform.
        var slip = new RoutingSlip([
            new RoutingSlipStep("Validate"),
            new RoutingSlipStep("Transform", "transform-topic"),
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

        // After executing "Validate", one step remains.
        Assert.That(result.StepName, Is.EqualTo("Validate"));
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.RemainingSlip.Steps, Has.Count.EqualTo(1));
        Assert.That(result.RemainingSlip.CurrentStep!.StepName, Is.EqualTo("Transform"));
        Assert.That(result.ForwardedToTopic, Is.Null); // No destination on Validate step.
    }

    // ── Step with Parameters ────────────────────────────────────────────────

    [Test]
    public async Task Execute_StepWithParameters_PassesParametersToHandler()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        IReadOnlyDictionary<string, string>? receivedParams = null;

        var handler = Substitute.For<IRoutingSlipStepHandler>();
        handler.StepName.Returns("Enrich");
        handler.HandleAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                receivedParams = ci.ArgAt<IReadOnlyDictionary<string, string>?>(1);
                return true;
            });

        var router = new RoutingSlipRouter(
            [handler], producer, NullLogger<RoutingSlipRouter>.Instance);

        var parameters = new Dictionary<string, string>
        {
            ["lookupUrl"] = "https://api.example.com/enrich",
            ["timeout"] = "30",
        };

        var slip = new RoutingSlip([
            new RoutingSlipStep("Enrich", null, parameters),
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
        Assert.That(receivedParams, Is.Not.Null);
        Assert.That(receivedParams!["lookupUrl"], Is.EqualTo("https://api.example.com/enrich"));
        Assert.That(receivedParams["timeout"], Is.EqualTo("30"));
    }

    // ── Handler Throws Exception — Step Fails Gracefully ────────────────────

    [Test]
    public async Task Execute_HandlerThrows_ResultIndicatesFailureWithMessage()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();

        var handler = Substitute.For<IRoutingSlipStepHandler>();
        handler.StepName.Returns("RiskyStep");
        handler.HandleAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>())
            .Returns<bool>(_ => throw new InvalidOperationException("Connection timed out"));

        var router = new RoutingSlipRouter(
            [handler], producer, NullLogger<RoutingSlipRouter>.Instance);

        var slip = new RoutingSlip([new RoutingSlipStep("RiskyStep", "output-topic")]);
        var envelope = IntegrationEnvelope<string>.Create(
            "payload", "Service", "event.type") with
        {
            Metadata = new Dictionary<string, string>
            {
                [RoutingSlip.MetadataKey] = JsonSerializer.Serialize(slip.Steps),
            },
        };

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Does.Contain("Connection timed out"));
        Assert.That(result.ForwardedToTopic, Is.Null);
    }

    // ── RoutingSlip Contract Tests ──────────────────────────────────────────

    [Test]
    public void RoutingSlip_Advance_ConsumesCurrentStep()
    {
        var slip = new RoutingSlip([
            new RoutingSlipStep("Step1"),
            new RoutingSlipStep("Step2"),
            new RoutingSlipStep("Step3"),
        ]);

        Assert.That(slip.IsComplete, Is.False);
        Assert.That(slip.CurrentStep!.StepName, Is.EqualTo("Step1"));

        var advanced = slip.Advance();
        Assert.That(advanced.CurrentStep!.StepName, Is.EqualTo("Step2"));
        Assert.That(advanced.Steps, Has.Count.EqualTo(2));

        var advanced2 = advanced.Advance();
        Assert.That(advanced2.CurrentStep!.StepName, Is.EqualTo("Step3"));

        var completed = advanced2.Advance();
        Assert.That(completed.IsComplete, Is.True);
        Assert.That(completed.CurrentStep, Is.Null);
    }
}
