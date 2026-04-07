// ============================================================================
// Tutorial 13 – Routing Slip (Lab)
// ============================================================================
// EIP Pattern: Routing Slip
// Real Integrations: Wire real RoutingSlipRouter with NatsBrokerEndpoint
// (real NATS JetStream via Aspire) as producer, execute steps sequentially,
// verify forwarding to destination topics.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial13;

[TestFixture]
public sealed class Lab
{
    // ── 1. Single-Step Execution ───────────────────────────────────────

    [Test]
    public async Task ExecuteStep_SingleStep_SucceedsAndForwards()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t13-single");
        var topic = AspireFixture.UniqueTopic("t13-validated");

        var router = CreateRouter(nats, new AlwaysSucceedHandler("Validate"));
        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Validate", topic));

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.StepName, Is.EqualTo("Validate"));
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.FailureReason, Is.Null);
        Assert.That(result.ForwardedToTopic, Is.EqualTo(topic));
        Assert.That(result.RemainingSlip.IsComplete, Is.True);
        nats.AssertReceivedOnTopic(topic, 1);
    }

    [Test]
    public async Task ExecuteStep_NoDestination_CompletesInProcess()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t13-nodest");

        var router = CreateRouter(nats, new AlwaysSucceedHandler("Enrich"));
        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Enrich"));

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ForwardedToTopic, Is.Null);
        nats.AssertNoneReceived();
    }

    // ── 2. Error Handling ──────────────────────────────────────────────

    [Test]
    public async Task ExecuteStep_HandlerFails_ReturnsFalseResult()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t13-fail");
        var topic = AspireFixture.UniqueTopic("t13-transformed");

        var router = CreateRouter(nats, new AlwaysFailHandler("Transform"));
        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Transform", topic));

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.StepName, Is.EqualTo("Transform"));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Is.Not.Null);
        nats.AssertNoneReceived();
    }

    [Test]
    public async Task ExecuteStep_NoHandlerRegistered_FailsGracefully()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t13-nohandler");
        var topic = AspireFixture.UniqueTopic("t13-dest");

        var router = CreateRouter(nats, new AlwaysSucceedHandler("Other"));
        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("NonExistent", topic));

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Does.Contain("NonExistent"));
        nats.AssertNoneReceived();
    }

    // ── 3. Multi-Step & Parameters ────────────────────────────────────

    [Test]
    public async Task ExecuteStep_MultiStepSlip_AdvancesCorrectly()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t13-multi");
        var step1Topic = AspireFixture.UniqueTopic("t13-step1");
        var step2Topic = AspireFixture.UniqueTopic("t13-step2");

        var router = CreateRouter(nats,
            new AlwaysSucceedHandler("Step1"),
            new AlwaysSucceedHandler("Step2"));

        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Step1", step1Topic),
            new RoutingSlipStep("Step2", step2Topic));

        var result1 = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result1.StepName, Is.EqualTo("Step1"));
        Assert.That(result1.Succeeded, Is.True);
        Assert.That(result1.RemainingSlip.Steps, Has.Count.EqualTo(1));
        Assert.That(result1.RemainingSlip.CurrentStep!.StepName, Is.EqualTo("Step2"));
        nats.AssertReceivedOnTopic(step1Topic, 1);
    }

    [Test]
    public async Task ExecuteStep_WithParameters_PassesParametersToHandler()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t13-params");
        var topic = AspireFixture.UniqueTopic("t13-configured");

        var handler = new ParameterCapturingHandler("Configure");
        var router = CreateRouter(nats, handler);

        var parameters = new Dictionary<string, string>
        {
            ["format"] = "json",
            ["compress"] = "true",
        };
        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Configure", topic, parameters));

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(handler.CapturedParameters, Is.Not.Null);
        Assert.That(handler.CapturedParameters!["format"], Is.EqualTo("json"));
        Assert.That(handler.CapturedParameters["compress"], Is.EqualTo("true"));
        nats.AssertReceivedOnTopic(topic, 1);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static RoutingSlipRouter CreateRouter(
        NatsBrokerEndpoint nats, params IRoutingSlipStepHandler[] handlers) =>
        new(handlers, nats, NullLogger<RoutingSlipRouter>.Instance);

    private static IntegrationEnvelope<string> CreateEnvelopeWithSlip(
        params RoutingSlipStep[] steps)
    {
        var slip = new RoutingSlip(steps.ToList().AsReadOnly());
        var slipJson = JsonSerializer.Serialize(slip.Steps, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        return IntegrationEnvelope<string>.Create("test-payload", "TestSvc", "test.event") with
        {
            Metadata = new Dictionary<string, string>
            {
                [RoutingSlip.MetadataKey] = slipJson,
            },
        };
    }

    // ── Test step handlers ──────────────────────────────────────────────

    private sealed class AlwaysSucceedHandler : IRoutingSlipStepHandler
    {
        public AlwaysSucceedHandler(string stepName) => StepName = stepName;
        public string StepName { get; }

        public Task<bool> HandleAsync<T>(
            IntegrationEnvelope<T> envelope,
            IReadOnlyDictionary<string, string>? parameters,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class AlwaysFailHandler : IRoutingSlipStepHandler
    {
        public AlwaysFailHandler(string stepName) => StepName = stepName;
        public string StepName { get; }

        public Task<bool> HandleAsync<T>(
            IntegrationEnvelope<T> envelope,
            IReadOnlyDictionary<string, string>? parameters,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class ParameterCapturingHandler : IRoutingSlipStepHandler
    {
        public ParameterCapturingHandler(string stepName) => StepName = stepName;
        public string StepName { get; }
        public IReadOnlyDictionary<string, string>? CapturedParameters { get; private set; }

        public Task<bool> HandleAsync<T>(
            IntegrationEnvelope<T> envelope,
            IReadOnlyDictionary<string, string>? parameters,
            CancellationToken cancellationToken = default)
        {
            CapturedParameters = parameters;
            return Task.FromResult(true);
        }
    }
}
