// ============================================================================
// Tutorial 16 – Transform Pipeline (Lab)
// ============================================================================
// This lab exercises the TransformPipeline — the pattern that chains an
// ordered sequence of ITransformStep instances. You will verify step
// execution order, disabled pipeline passthrough, payload size limits,
// stop-on-failure behaviour, and metadata accumulation.
// ============================================================================

using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace TutorialLabs.Tutorial16;

[TestFixture]
public sealed class Lab
{
    // ── Basic Pipeline Execution ────────────────────────────────────────────

    [Test]
    public async Task Execute_SingleStep_AppliesTransformation()
    {
        var step = Substitute.For<ITransformStep>();
        step.Name.Returns("Upper");
        step.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ctx = ci.Arg<TransformContext>();
                return ctx.WithPayload(ctx.Payload.ToUpperInvariant());
            });

        var options = Options.Create(new TransformOptions());
        var pipeline = new TransformPipeline(
            new[] { step }, options, NullLogger<TransformPipeline>.Instance);

        var result = await pipeline.ExecuteAsync("hello", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("HELLO"));
        Assert.That(result.StepsApplied, Is.EqualTo(1));
        Assert.That(result.ContentType, Is.EqualTo("text/plain"));
    }

    [Test]
    public async Task Execute_MultipleSteps_AppliedInOrder()
    {
        var step1 = Substitute.For<ITransformStep>();
        step1.Name.Returns("Append-A");
        step1.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<TransformContext>().WithPayload(ci.Arg<TransformContext>().Payload + "A"));

        var step2 = Substitute.For<ITransformStep>();
        step2.Name.Returns("Append-B");
        step2.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<TransformContext>().WithPayload(ci.Arg<TransformContext>().Payload + "B"));

        var options = Options.Create(new TransformOptions());
        var pipeline = new TransformPipeline(
            new[] { step1, step2 }, options, NullLogger<TransformPipeline>.Instance);

        var result = await pipeline.ExecuteAsync("X", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("XAB"));
        Assert.That(result.StepsApplied, Is.EqualTo(2));
    }

    // ── Disabled Pipeline ───────────────────────────────────────────────────

    [Test]
    public async Task Execute_DisabledPipeline_ReturnsInputUnchanged()
    {
        var step = Substitute.For<ITransformStep>();

        var options = Options.Create(new TransformOptions { Enabled = false });
        var pipeline = new TransformPipeline(
            new[] { step }, options, NullLogger<TransformPipeline>.Instance);

        var result = await pipeline.ExecuteAsync("{\"id\":1}", "application/json");

        Assert.That(result.Payload, Is.EqualTo("{\"id\":1}"));
        Assert.That(result.StepsApplied, Is.EqualTo(0));
        await step.DidNotReceive()
            .ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>());
    }

    // ── Max Payload Size ────────────────────────────────────────────────────

    [Test]
    public void Execute_PayloadExceedsMaxSize_ThrowsInvalidOperationException()
    {
        var options = Options.Create(new TransformOptions { MaxPayloadSizeBytes = 10 });
        var pipeline = new TransformPipeline(
            Array.Empty<ITransformStep>(), options, NullLogger<TransformPipeline>.Instance);

        var largePayload = new string('x', 50);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => pipeline.ExecuteAsync(largePayload, "text/plain"));
    }

    // ── Stop On Step Failure ────────────────────────────────────────────────

    [Test]
    public async Task Execute_StepFails_StopOnFailureFalse_ContinuesExecution()
    {
        var failingStep = Substitute.For<ITransformStep>();
        failingStep.Name.Returns("Failing");
        failingStep.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("step error"));

        var goodStep = Substitute.For<ITransformStep>();
        goodStep.Name.Returns("Good");
        goodStep.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<TransformContext>().WithPayload("done"));

        var options = Options.Create(new TransformOptions { StopOnStepFailure = false });
        var pipeline = new TransformPipeline(
            new[] { failingStep, goodStep }, options, NullLogger<TransformPipeline>.Instance);

        var result = await pipeline.ExecuteAsync("input", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("done"));
        Assert.That(result.StepsApplied, Is.EqualTo(1));
    }

    [Test]
    public void Execute_StepFails_StopOnFailureTrue_Throws()
    {
        var failingStep = Substitute.For<ITransformStep>();
        failingStep.Name.Returns("Failing");
        failingStep.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var options = Options.Create(new TransformOptions { StopOnStepFailure = true });
        var pipeline = new TransformPipeline(
            new[] { failingStep }, options, NullLogger<TransformPipeline>.Instance);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => pipeline.ExecuteAsync("input", "text/plain"));
    }

    // ── Metadata Accumulation ───────────────────────────────────────────────

    [Test]
    public async Task Execute_StepsWriteMetadata_MetadataAccumulatedInResult()
    {
        var step = Substitute.For<ITransformStep>();
        step.Name.Returns("MetaStep");
        step.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ctx = ci.Arg<TransformContext>();
                ctx.Metadata["custom-key"] = "custom-value";
                return ctx;
            });

        var options = Options.Create(new TransformOptions());
        var pipeline = new TransformPipeline(
            new[] { step }, options, NullLogger<TransformPipeline>.Instance);

        var result = await pipeline.ExecuteAsync("data", "text/plain");

        Assert.That(result.Metadata.ContainsKey("custom-key"), Is.True);
        Assert.That(result.Metadata["custom-key"], Is.EqualTo("custom-value"));
    }
}
