// ============================================================================
// Tutorial 13 – Routing Slip (Lab)
// ============================================================================
// EIP Pattern: Routing Slip
// E2E: Wire real RoutingSlipRouter with test step handlers + MockEndpoint,
// execute steps sequentially, verify forwarding to destination topics.
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
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("routing-slip-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    [Test]
    public async Task ExecuteStep_SingleStep_SucceedsAndForwards()
    {
        var router = CreateRouter(new AlwaysSucceedHandler("Validate"));
        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Validate", "validated-topic"));

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.StepName, Is.EqualTo("Validate"));
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.FailureReason, Is.Null);
        Assert.That(result.ForwardedToTopic, Is.EqualTo("validated-topic"));
        Assert.That(result.RemainingSlip.IsComplete, Is.True);
        _output.AssertReceivedOnTopic("validated-topic", 1);
    }

    [Test]
    public async Task ExecuteStep_NoDestination_CompletesInProcess()
    {
        var router = CreateRouter(new AlwaysSucceedHandler("Enrich"));
        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Enrich"));

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ForwardedToTopic, Is.Null);
        _output.AssertNoneReceived();
    }

    [Test]
    public async Task ExecuteStep_HandlerFails_ReturnsFalseResult()
    {
        var router = CreateRouter(new AlwaysFailHandler("Transform"));
        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Transform", "transformed-topic"));

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.StepName, Is.EqualTo("Transform"));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Is.Not.Null);
        _output.AssertNoneReceived();
    }

    [Test]
    public async Task ExecuteStep_NoHandlerRegistered_FailsGracefully()
    {
        var router = CreateRouter(new AlwaysSucceedHandler("Other"));
        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("NonExistent", "dest-topic"));

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Does.Contain("NonExistent"));
        _output.AssertNoneReceived();
    }

    [Test]
    public async Task ExecuteStep_MultiStepSlip_AdvancesCorrectly()
    {
        var router = CreateRouter(
            new AlwaysSucceedHandler("Step1"),
            new AlwaysSucceedHandler("Step2"));

        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Step1", "step1-out"),
            new RoutingSlipStep("Step2", "step2-out"));

        var result1 = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result1.StepName, Is.EqualTo("Step1"));
        Assert.That(result1.Succeeded, Is.True);
        Assert.That(result1.RemainingSlip.Steps, Has.Count.EqualTo(1));
        Assert.That(result1.RemainingSlip.CurrentStep!.StepName, Is.EqualTo("Step2"));
        _output.AssertReceivedOnTopic("step1-out", 1);
    }

    [Test]
    public async Task ExecuteStep_WithParameters_PassesParametersToHandler()
    {
        var handler = new ParameterCapturingHandler("Configure");
        var router = CreateRouter(handler);

        var parameters = new Dictionary<string, string>
        {
            ["format"] = "json",
            ["compress"] = "true",
        };
        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Configure", "configured-topic", parameters));

        var result = await router.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(handler.CapturedParameters, Is.Not.Null);
        Assert.That(handler.CapturedParameters!["format"], Is.EqualTo("json"));
        Assert.That(handler.CapturedParameters["compress"], Is.EqualTo("true"));
        _output.AssertReceivedOnTopic("configured-topic", 1);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private RoutingSlipRouter CreateRouter(params IRoutingSlipStepHandler[] handlers) =>
        new(handlers, _output, NullLogger<RoutingSlipRouter>.Instance);

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
