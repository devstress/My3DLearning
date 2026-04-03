using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class TransformPipelineTests
{
    // ------------------------------------------------------------------ //
    // Basic pipeline execution
    // ------------------------------------------------------------------ //

    [Test]
    public async Task ExecuteAsync_NoSteps_ReturnsInputUnchanged()
    {
        var sut = BuildPipeline([], new TransformOptions());

        var result = await sut.ExecuteAsync("{}", "application/json");

        Assert.That(result.Payload, Is.EqualTo("{}"));
        Assert.That(result.ContentType, Is.EqualTo("application/json"));
        Assert.That(result.StepsApplied, Is.EqualTo(0));
    }

    [Test]
    public async Task ExecuteAsync_SingleStep_AppliesTransformation()
    {
        var step = Substitute.For<ITransformStep>();
        step.Name.Returns("TestStep");
        step.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ctx = ci.Arg<TransformContext>();
                return ctx.WithPayload(ctx.Payload.ToUpperInvariant());
            });

        var sut = BuildPipeline([step], new TransformOptions());

        var result = await sut.ExecuteAsync("hello", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("HELLO"));
        Assert.That(result.StepsApplied, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_MultipleSteps_ExecutesInOrder()
    {
        var step1 = Substitute.For<ITransformStep>();
        step1.Name.Returns("Step1");
        step1.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ctx = ci.Arg<TransformContext>();
                return ctx.WithPayload(ctx.Payload + "-A");
            });

        var step2 = Substitute.For<ITransformStep>();
        step2.Name.Returns("Step2");
        step2.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ctx = ci.Arg<TransformContext>();
                return ctx.WithPayload(ctx.Payload + "-B");
            });

        var sut = BuildPipeline([step1, step2], new TransformOptions());

        var result = await sut.ExecuteAsync("start", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("start-A-B"));
        Assert.That(result.StepsApplied, Is.EqualTo(2));
    }

    // ------------------------------------------------------------------ //
    // Disabled pipeline
    // ------------------------------------------------------------------ //

    [Test]
    public async Task ExecuteAsync_Disabled_ReturnsInputUnchanged()
    {
        var step = Substitute.For<ITransformStep>();
        step.Name.Returns("Blocked");
        var sut = BuildPipeline([step], new TransformOptions { Enabled = false });

        var result = await sut.ExecuteAsync("data", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("data"));
        Assert.That(result.StepsApplied, Is.EqualTo(0));
        await step.DidNotReceive().ExecuteAsync(
            Arg.Any<TransformContext>(), Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------------ //
    // Max payload size
    // ------------------------------------------------------------------ //

    [Test]
    public void ExecuteAsync_PayloadExceedsMaxSize_ThrowsInvalidOperationException()
    {
        var sut = BuildPipeline([], new TransformOptions { MaxPayloadSizeBytes = 5 });

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.ExecuteAsync("too-long-payload", "text/plain"));
    }

    [Test]
    public async Task ExecuteAsync_PayloadWithinMaxSize_Succeeds()
    {
        var sut = BuildPipeline([], new TransformOptions { MaxPayloadSizeBytes = 100 });

        var result = await sut.ExecuteAsync("short", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("short"));
    }

    [Test]
    public async Task ExecuteAsync_MaxPayloadSizeZero_NoLimit()
    {
        var sut = BuildPipeline([], new TransformOptions { MaxPayloadSizeBytes = 0 });
        var bigPayload = new string('x', 10_000);

        var result = await sut.ExecuteAsync(bigPayload, "text/plain");

        Assert.That(result.Payload.Length, Is.EqualTo(10_000));
    }

    // ------------------------------------------------------------------ //
    // StopOnStepFailure behaviour
    // ------------------------------------------------------------------ //

    [Test]
    public void ExecuteAsync_StepFailsWithStopOnFailureTrue_ThrowsException()
    {
        var step = Substitute.For<ITransformStep>();
        step.Name.Returns("FailStep");
        step.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var sut = BuildPipeline([step], new TransformOptions { StopOnStepFailure = true });

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.ExecuteAsync("data", "text/plain"));
    }

    [Test]
    public async Task ExecuteAsync_StepFailsWithStopOnFailureFalse_SkipsAndContinues()
    {
        var failStep = Substitute.For<ITransformStep>();
        failStep.Name.Returns("FailStep");
        failStep.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var goodStep = Substitute.For<ITransformStep>();
        goodStep.Name.Returns("GoodStep");
        goodStep.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ctx = ci.Arg<TransformContext>();
                return ctx.WithPayload("transformed");
            });

        var sut = BuildPipeline([failStep, goodStep],
            new TransformOptions { StopOnStepFailure = false });

        var result = await sut.ExecuteAsync("data", "text/plain");

        Assert.That(result.Payload, Is.EqualTo("transformed"));
        Assert.That(result.StepsApplied, Is.EqualTo(1));
    }

    // ------------------------------------------------------------------ //
    // Cancellation
    // ------------------------------------------------------------------ //

    [Test]
    public void ExecuteAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var step = Substitute.For<ITransformStep>();
        step.Name.Returns("Slow");

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = BuildPipeline([step], new TransformOptions());

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await sut.ExecuteAsync("data", "text/plain", cts.Token));
    }

    [Test]
    public void ExecuteAsync_StepThrowsOperationCanceled_PropagatesEvenIfStopOnFailureFalse()
    {
        var step = Substitute.For<ITransformStep>();
        step.Name.Returns("Cancel");
        step.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var sut = BuildPipeline([step], new TransformOptions { StopOnStepFailure = false });

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await sut.ExecuteAsync("data", "text/plain"));
    }

    // ------------------------------------------------------------------ //
    // Guard clauses
    // ------------------------------------------------------------------ //

    [Test]
    public void ExecuteAsync_NullPayload_ThrowsArgumentNullException()
    {
        var sut = BuildPipeline([], new TransformOptions());

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await sut.ExecuteAsync(null!, "text/plain"));
    }

    [Test]
    public void ExecuteAsync_EmptyContentType_ThrowsArgumentException()
    {
        var sut = BuildPipeline([], new TransformOptions());

        Assert.ThrowsAsync<ArgumentException>(
            async () => await sut.ExecuteAsync("data", ""));
    }

    // ------------------------------------------------------------------ //
    // Metadata propagation
    // ------------------------------------------------------------------ //

    [Test]
    public async Task ExecuteAsync_StepSetsMetadata_PropagatedInResult()
    {
        var step = Substitute.For<ITransformStep>();
        step.Name.Returns("MetaStep");
        step.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ctx = ci.Arg<TransformContext>();
                ctx.Metadata["key1"] = "value1";
                return ctx;
            });

        var sut = BuildPipeline([step], new TransformOptions());

        var result = await sut.ExecuteAsync("data", "text/plain");

        Assert.That(result.Metadata.ContainsKey("key1"), Is.True);
        Assert.That(result.Metadata["key1"], Is.EqualTo("value1"));
    }

    // ------------------------------------------------------------------ //
    // Helper
    // ------------------------------------------------------------------ //

    private static TransformPipeline BuildPipeline(
        ITransformStep[] steps,
        TransformOptions options) =>
        new(steps, Options.Create(options),
            NullLogger<TransformPipeline>.Instance);
}
