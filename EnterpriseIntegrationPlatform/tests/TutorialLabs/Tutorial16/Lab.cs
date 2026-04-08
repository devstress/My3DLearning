// ============================================================================
// Tutorial 16 – Transform Pipeline (Lab · Guided Practice)
// ============================================================================
// PURPOSE: Run each test in order to see how the Transform Pipeline pattern
//          chains ordered ITransformStep instances to transform payloads
//          in sequence.
//
// CONCEPTS DEMONSTRATED (one per test):
//   1. Pipeline_SingleStep_TransformsPayload           — single step transforms payload and reports StepsApplied
//   2. Pipeline_MultipleSteps_ChainsTransformations    — multiple steps chain transformations in order
//   3. Pipeline_Disabled_ReturnsInputUnchanged         — disabled pipeline returns input unchanged
//   4. Pipeline_StepFailure_SkippedWhenNotStopOnFailure — step failure skipped when StopOnStepFailure is false
//   5. Pipeline_MaxPayloadSize_RejectsOversized        — payload exceeding max size throws InvalidOperationException
//   6. Pipeline_E2E_PublishTransformedToNatsEndpoint    — end-to-end transform and publish via real NATS
//
// INFRASTRUCTURE: NatsBrokerEndpoint (real NATS JetStream via Aspire)
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial16;

/// <summary>Test step that converts the payload to upper-case.</summary>
internal sealed class UpperCaseStep : ITransformStep
{
    public string Name => "UpperCase";

    public Task<TransformContext> ExecuteAsync(
        TransformContext context, CancellationToken cancellationToken = default)
    {
        var result = context.WithPayload(context.Payload.ToUpperInvariant());
        result.Metadata[$"Step.{Name}.Applied"] = "true";
        return Task.FromResult(result);
    }
}

/// <summary>Test step that prepends a configurable prefix to the payload.</summary>
internal sealed class PrefixStep : ITransformStep
{
    private readonly string _prefix;
    public PrefixStep(string prefix) => _prefix = prefix;
    public string Name => "Prefix";

    public Task<TransformContext> ExecuteAsync(
        TransformContext context, CancellationToken cancellationToken = default)
    {
        var result = context.WithPayload($"{_prefix}{context.Payload}");
        result.Metadata[$"Step.{Name}.Applied"] = "true";
        return Task.FromResult(result);
    }
}

/// <summary>Test step that always throws to verify error-handling behaviour.</summary>
internal sealed class FailingStep : ITransformStep
{
    public string Name => "Failing";

    public Task<TransformContext> ExecuteAsync(
        TransformContext context, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Intentional step failure");
}

[TestFixture]
public sealed class Lab
{
    // ── 1. Single & Multi-Step Transforms ────────────────────────────

    [Test]
    public async Task Pipeline_SingleStep_TransformsPayload()
    {
        var pipeline = CreatePipeline(new ITransformStep[] { new UpperCaseStep() });

        var result = await pipeline.ExecuteAsync("hello world", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("HELLO WORLD"));
        Assert.That(result.StepsApplied, Is.EqualTo(1));
        Assert.That(result.ContentType, Is.EqualTo("text/plain"));
    }

    [Test]
    public async Task Pipeline_MultipleSteps_ChainsTransformations()
    {
        var pipeline = CreatePipeline(new ITransformStep[]
        {
            new UpperCaseStep(),
            new PrefixStep("[TRANSFORMED] "),
        });

        var result = await pipeline.ExecuteAsync("order data", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("[TRANSFORMED] ORDER DATA"));
        Assert.That(result.StepsApplied, Is.EqualTo(2));
    }


    // ── 2. Pipeline Configuration & Error Handling ───────────────────

    [Test]
    public async Task Pipeline_Disabled_ReturnsInputUnchanged()
    {
        var options = Options.Create(new TransformOptions { Enabled = false });
        var pipeline = new TransformPipeline(
            new ITransformStep[] { new UpperCaseStep() }, options,
            NullLogger<TransformPipeline>.Instance);

        var result = await pipeline.ExecuteAsync("keep me", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("keep me"));
        Assert.That(result.StepsApplied, Is.EqualTo(0));
    }

    [Test]
    public async Task Pipeline_StepFailure_SkippedWhenNotStopOnFailure()
    {
        var options = Options.Create(new TransformOptions
        {
            Enabled = true,
            StopOnStepFailure = false,
        });
        var pipeline = new TransformPipeline(
            new ITransformStep[] { new FailingStep(), new UpperCaseStep() },
            options, NullLogger<TransformPipeline>.Instance);

        var result = await pipeline.ExecuteAsync("hello", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("HELLO"));
        Assert.That(result.StepsApplied, Is.EqualTo(1));
    }

    [Test]
    public void Pipeline_MaxPayloadSize_RejectsOversized()
    {
        var options = Options.Create(new TransformOptions
        {
            Enabled = true,
            MaxPayloadSizeBytes = 5,
        });
        var pipeline = new TransformPipeline(
            new ITransformStep[] { new UpperCaseStep() }, options,
            NullLogger<TransformPipeline>.Instance);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => pipeline.ExecuteAsync("this is too long", "text/plain"));
    }


    // ── 3. End-to-End Integration (Real NATS) ────────────────────────

    [Test]
    public async Task Pipeline_E2E_PublishTransformedToNatsEndpoint()
    {
        await using var nats = AspireFixture.CreateNatsEndpoint("t16-e2e");
        var topic = AspireFixture.UniqueTopic("t16-transformed");

        var pipeline = CreatePipeline(new ITransformStep[]
        {
            new UpperCaseStep(),
            new PrefixStep("MSG:"),
        });

        var result = await pipeline.ExecuteAsync("{\"name\":\"test\"}", "application/json");

        var envelope = IntegrationEnvelope<string>.Create(
            result.Payload, "TransformService", "transform.completed");
        await nats.PublishAsync(envelope, topic, CancellationToken.None);

        nats.AssertReceivedOnTopic(topic, 1);
        var received = nats.GetReceived<string>();
        Assert.That(received.Payload, Does.StartWith("MSG:"));
    }

    private static TransformPipeline CreatePipeline(ITransformStep[] steps)
    {
        var options = Options.Create(new TransformOptions { Enabled = true });
        return new TransformPipeline(steps, options, NullLogger<TransformPipeline>.Instance);
    }
}
