// ============================================================================
// Tutorial 13 – Routing Slip (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — Execute a full three-step pipeline sequentially
//   🟡 Intermediate  — Detect partial failure mid-slip and verify it halts
//   🔴 Advanced      — Handle a missing routing slip with the correct exception
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial13;

[TestFixture]
public sealed class Exam
{
    // ── 🟢 STARTER — Full pipeline executes all steps sequentially ────
    //
    // SCENARIO: A three-step routing slip (Validate → Transform → Deliver)
    //           is attached to a message. Each step succeeds and forwards
    //           to its destination topic.
    //
    // WHAT YOU PROVE: The router executes steps in order, advances the
    //                 slip after each one, and publishes to every topic.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Starter_FullPipeline_ExecutesAllStepsSequentially()
    {
        await using var output = new MockEndpoint("pipeline");
        // TODO: Create a IRoutingSlipStepHandler with appropriate configuration
        dynamic handlers = null!;
        // TODO: Create a RoutingSlipRouter with appropriate configuration
        dynamic router = null!;

        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Validate", "step1-out"),
            new RoutingSlipStep("Transform", "step2-out"),
            new RoutingSlipStep("Deliver", "step3-out"));

        // TODO: var r1 = await router.ExecuteCurrentStepAsync(...)
        dynamic r1 = null!;
        Assert.That(r1.Succeeded, Is.True);
        Assert.That(r1.StepName, Is.EqualTo("Validate"));
        Assert.That(r1.RemainingSlip.Steps, Has.Count.EqualTo(2));

        // TODO: var r2 = await router.ExecuteCurrentStepAsync(...)
        dynamic r2 = null!;
        Assert.That(r2.Succeeded, Is.True);
        Assert.That(r2.StepName, Is.EqualTo("Transform"));
        Assert.That(r2.RemainingSlip.Steps, Has.Count.EqualTo(1));

        // TODO: var r3 = await router.ExecuteCurrentStepAsync(...)
        dynamic r3 = null!;
        Assert.That(r3.Succeeded, Is.True);
        Assert.That(r3.StepName, Is.EqualTo("Deliver"));
        Assert.That(r3.RemainingSlip.IsComplete, Is.True);

        output.AssertReceivedCount(3);
        output.AssertReceivedOnTopic("step1-out", 1);
        output.AssertReceivedOnTopic("step2-out", 1);
        output.AssertReceivedOnTopic("step3-out", 1);
    }

    // ── 🟡 INTERMEDIATE — Partial failure stops at the failed step ────
    //
    // SCENARIO: A three-step slip where the second handler (Transform)
    //           always fails. The first step succeeds and forwards, but
    //           the pipeline halts at the failing step.
    //
    // WHAT YOU PROVE: The router stops processing when a step fails and
    //                 does not forward to subsequent destination topics.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Intermediate_PartialFailure_StopsAtFailedStep()
    {
        await using var output = new MockEndpoint("partial-fail");
        // TODO: Create a IRoutingSlipStepHandler with appropriate configuration
        dynamic handlers = null!;
        // TODO: Create a RoutingSlipRouter with appropriate configuration
        dynamic router = null!;

        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Validate", "step1-out"),
            new RoutingSlipStep("Transform", "step2-out"),
            new RoutingSlipStep("Deliver", "step3-out"));

        // TODO: var r1 = await router.ExecuteCurrentStepAsync(...)
        dynamic r1 = null!;
        Assert.That(r1.Succeeded, Is.True);
        output.AssertReceivedOnTopic("step1-out", 1);

        // TODO: var r2 = await router.ExecuteCurrentStepAsync(...)
        dynamic r2 = null!;
        Assert.That(r2.Succeeded, Is.False);
        Assert.That(r2.StepName, Is.EqualTo("Transform"));
        Assert.That(r2.FailureReason, Is.Not.Null);

        // Only step1 was forwarded; Transform failed so step2/step3 not reached
        output.AssertReceivedCount(1);
    }

    // ── 🔴 ADVANCED — Missing routing slip throws InvalidOperation ────
    //
    // SCENARIO: An envelope arrives without any routing slip metadata.
    //           The router must detect the absence and throw rather than
    //           silently skip processing.
    //
    // WHAT YOU PROVE: The router validates that a routing slip is present
    //                 and raises InvalidOperationException when it is not.
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Advanced_MissingSlip_ThrowsInvalidOperation()
    {
        await using var output = new MockEndpoint("no-slip");
        // TODO: Create a RoutingSlipRouter with appropriate configuration
        dynamic router = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;

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
#endif
