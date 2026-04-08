// ============================================================================
// Tutorial 13 – Routing Slip (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — Execute a full three-step pipeline sequentially
//   🟡 Intermediate — Detect partial failure mid-slip and verify it halts
//   🔴 Advanced     — Handle a missing routing slip with the correct exception
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial13;

[TestFixture]
public sealed class ExamAnswers
{
    // ── 🟢 STARTER — Full pipeline executes all steps sequentially ────

    [Test]
    public async Task Starter_FullPipeline_ExecutesAllStepsSequentially()
    {
        await using var output = new MockEndpoint("pipeline");
        var handlers = new IRoutingSlipStepHandler[]
        {
            new TrackingHandler("Validate"),
            new TrackingHandler("Transform"),
            new TrackingHandler("Deliver"),
        };
        var router = new RoutingSlipRouter(
            handlers, output, NullLogger<RoutingSlipRouter>.Instance);

        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Validate", "step1-out"),
            new RoutingSlipStep("Transform", "step2-out"),
            new RoutingSlipStep("Deliver", "step3-out"));

        var r1 = await router.ExecuteCurrentStepAsync(envelope);
        Assert.That(r1.Succeeded, Is.True);
        Assert.That(r1.StepName, Is.EqualTo("Validate"));
        Assert.That(r1.RemainingSlip.Steps, Has.Count.EqualTo(2));

        var r2 = await router.ExecuteCurrentStepAsync(envelope);
        Assert.That(r2.Succeeded, Is.True);
        Assert.That(r2.StepName, Is.EqualTo("Transform"));
        Assert.That(r2.RemainingSlip.Steps, Has.Count.EqualTo(1));

        var r3 = await router.ExecuteCurrentStepAsync(envelope);
        Assert.That(r3.Succeeded, Is.True);
        Assert.That(r3.StepName, Is.EqualTo("Deliver"));
        Assert.That(r3.RemainingSlip.IsComplete, Is.True);

        output.AssertReceivedCount(3);
        output.AssertReceivedOnTopic("step1-out", 1);
        output.AssertReceivedOnTopic("step2-out", 1);
        output.AssertReceivedOnTopic("step3-out", 1);
    }

    // ── 🟡 INTERMEDIATE — Partial failure stops at the failed step ────

    [Test]
    public async Task Intermediate_PartialFailure_StopsAtFailedStep()
    {
        await using var output = new MockEndpoint("partial-fail");
        var handlers = new IRoutingSlipStepHandler[]
        {
            new TrackingHandler("Validate"),
            new FailingHandler("Transform"),
            new TrackingHandler("Deliver"),
        };
        var router = new RoutingSlipRouter(
            handlers, output, NullLogger<RoutingSlipRouter>.Instance);

        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Validate", "step1-out"),
            new RoutingSlipStep("Transform", "step2-out"),
            new RoutingSlipStep("Deliver", "step3-out"));

        var r1 = await router.ExecuteCurrentStepAsync(envelope);
        Assert.That(r1.Succeeded, Is.True);
        output.AssertReceivedOnTopic("step1-out", 1);

        var r2 = await router.ExecuteCurrentStepAsync(envelope);
        Assert.That(r2.Succeeded, Is.False);
        Assert.That(r2.StepName, Is.EqualTo("Transform"));
        Assert.That(r2.FailureReason, Is.Not.Null);

        // Only step1 was forwarded; Transform failed so step2/step3 not reached
        output.AssertReceivedCount(1);
    }

    // ── 🔴 ADVANCED — Missing routing slip throws InvalidOperation ────

    [Test]
    public async Task Advanced_MissingSlip_ThrowsInvalidOperation()
    {
        await using var output = new MockEndpoint("no-slip");
        var router = new RoutingSlipRouter(
            Array.Empty<IRoutingSlipStepHandler>(),
            output, NullLogger<RoutingSlipRouter>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "data", "svc", "test.event");

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await router.ExecuteCurrentStepAsync(envelope));
        output.AssertNoneReceived();
    }

    // ── Helpers ─────────────────────────────────────────────────────────

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

    private sealed class TrackingHandler : IRoutingSlipStepHandler
    {
        public TrackingHandler(string stepName) => StepName = stepName;
        public string StepName { get; }

        public Task<bool> HandleAsync<T>(
            IntegrationEnvelope<T> envelope,
            IReadOnlyDictionary<string, string>? parameters,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class FailingHandler : IRoutingSlipStepHandler
    {
        public FailingHandler(string stepName) => StepName = stepName;
        public string StepName { get; }

        public Task<bool> HandleAsync<T>(
            IntegrationEnvelope<T> envelope,
            IReadOnlyDictionary<string, string>? parameters,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
