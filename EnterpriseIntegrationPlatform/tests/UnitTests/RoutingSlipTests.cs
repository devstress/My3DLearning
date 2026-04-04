using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class RoutingSlipTests
{
    [Test]
    public void IsComplete_EmptySteps_ReturnsTrue()
    {
        var slip = new RoutingSlip([]);
        Assert.That(slip.IsComplete, Is.True);
    }

    [Test]
    public void IsComplete_WithSteps_ReturnsFalse()
    {
        var slip = new RoutingSlip([new RoutingSlipStep("Validate")]);
        Assert.That(slip.IsComplete, Is.False);
    }

    [Test]
    public void CurrentStep_WithSteps_ReturnsFirst()
    {
        var step = new RoutingSlipStep("Validate");
        var slip = new RoutingSlip([step, new RoutingSlipStep("Transform")]);

        Assert.That(slip.CurrentStep, Is.EqualTo(step));
    }

    [Test]
    public void CurrentStep_EmptySlip_ReturnsNull()
    {
        var slip = new RoutingSlip([]);
        Assert.That(slip.CurrentStep, Is.Null);
    }

    [Test]
    public void Advance_RemovesFirstStep()
    {
        var slip = new RoutingSlip(
        [
            new RoutingSlipStep("Validate"),
            new RoutingSlipStep("Transform"),
            new RoutingSlipStep("Deliver"),
        ]);

        var advanced = slip.Advance();

        Assert.That(advanced.Steps, Has.Count.EqualTo(2));
        Assert.That(advanced.CurrentStep!.StepName, Is.EqualTo("Transform"));
    }

    [Test]
    public void Advance_SingleStep_ResultsInCompleteSlip()
    {
        var slip = new RoutingSlip([new RoutingSlipStep("Deliver")]);
        var advanced = slip.Advance();

        Assert.That(advanced.IsComplete, Is.True);
        Assert.That(advanced.Steps, Has.Count.EqualTo(0));
    }

    [Test]
    public void Advance_CompleteSlip_ThrowsInvalidOperationException()
    {
        var slip = new RoutingSlip([]);

        Assert.Throws<InvalidOperationException>(() => slip.Advance());
    }

    [Test]
    public void RoutingSlipStep_Parameters_AreAccessible()
    {
        var parameters = new Dictionary<string, string> { ["format"] = "JSON", ["target"] = "ERP" };
        var step = new RoutingSlipStep("Transform", "output.topic", parameters);

        Assert.That(step.StepName, Is.EqualTo("Transform"));
        Assert.That(step.DestinationTopic, Is.EqualTo("output.topic"));
        Assert.That(step.Parameters, Has.Count.EqualTo(2));
        Assert.That(step.Parameters!["format"], Is.EqualTo("JSON"));
    }

    [Test]
    public void MetadataKey_IsRoutingSlip()
    {
        Assert.That(RoutingSlip.MetadataKey, Is.EqualTo("RoutingSlip"));
    }
}

[TestFixture]
public class RoutingSlipRouterTests
{
    private IMessageBrokerProducer _producer = null!;
    private IRoutingSlipStepHandler _validateHandler = null!;
    private IRoutingSlipStepHandler _transformHandler = null!;
    private IRoutingSlipStepHandler _deliverHandler = null!;
    private RoutingSlipRouter _sut = null!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _validateHandler = Substitute.For<IRoutingSlipStepHandler>();
        _transformHandler = Substitute.For<IRoutingSlipStepHandler>();
        _deliverHandler = Substitute.For<IRoutingSlipStepHandler>();

        _validateHandler.StepName.Returns("Validate");
        _transformHandler.StepName.Returns("Transform");
        _deliverHandler.StepName.Returns("Deliver");

        _sut = new RoutingSlipRouter(
            [_validateHandler, _transformHandler, _deliverHandler],
            _producer,
            NullLogger<RoutingSlipRouter>.Instance);
    }

    private static IntegrationEnvelope<string> CreateEnvelopeWithSlip(
        params RoutingSlipStep[] steps)
    {
        var slip = new RoutingSlip(steps.ToList().AsReadOnly());
        var slipJson = JsonSerializer.Serialize(slip.Steps, JsonOptions);

        return new IntegrationEnvelope<string>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "Test",
            MessageType = "OrderCreated",
            Payload = """{"orderId":1}""",
            Metadata = new Dictionary<string, string>
            {
                [RoutingSlip.MetadataKey] = slipJson,
            },
        };
    }

    [Test]
    public async Task ExecuteCurrentStepAsync_ValidStep_ExecutesAndAdvancesSlip()
    {
        _validateHandler.HandleAsync<string>(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>()).Returns(true);

        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Validate"),
            new RoutingSlipStep("Transform"));

        var result = await _sut.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.StepName, Is.EqualTo("Validate"));
        Assert.That(result.RemainingSlip.Steps, Has.Count.EqualTo(1));
        Assert.That(result.RemainingSlip.CurrentStep!.StepName, Is.EqualTo("Transform"));
    }

    [Test]
    public async Task ExecuteCurrentStepAsync_StepWithDestinationTopic_ForwardsMessage()
    {
        _validateHandler.HandleAsync<string>(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>()).Returns(true);

        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Validate", "transform.queue"),
            new RoutingSlipStep("Transform"));

        var result = await _sut.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.ForwardedToTopic, Is.EqualTo("transform.queue"));
        await _producer.Received(1).PublishAsync(envelope, "transform.queue", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteCurrentStepAsync_InProcessStep_DoesNotForward()
    {
        _validateHandler.HandleAsync<string>(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>()).Returns(true);

        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Validate"),
            new RoutingSlipStep("Transform"));

        var result = await _sut.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.ForwardedToTopic, Is.Null);
        await _producer.DidNotReceiveWithAnyArgs()
            .PublishAsync<string>(default!, default!, default);
    }

    [Test]
    public async Task ExecuteCurrentStepAsync_LastStep_CompletesSlip()
    {
        _deliverHandler.HandleAsync<string>(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>()).Returns(true);

        var envelope = CreateEnvelopeWithSlip(new RoutingSlipStep("Deliver"));

        var result = await _sut.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.RemainingSlip.IsComplete, Is.True);
    }

    [Test]
    public async Task ExecuteCurrentStepAsync_HandlerReturnsFalse_ReportsFailure()
    {
        _validateHandler.HandleAsync<string>(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>()).Returns(false);

        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Validate"),
            new RoutingSlipStep("Transform"));

        var result = await _sut.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Does.Contain("returned failure"));
        Assert.That(result.RemainingSlip.Steps, Has.Count.EqualTo(2), "slip not advanced on failure");
    }

    [Test]
    public async Task ExecuteCurrentStepAsync_HandlerThrows_ReportsFailure()
    {
        _validateHandler.HandleAsync<string>(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("Handler crashed"));

        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Validate"),
            new RoutingSlipStep("Transform"));

        var result = await _sut.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("Handler crashed"));
    }

    [Test]
    public async Task ExecuteCurrentStepAsync_NoHandlerRegistered_ReportsFailure()
    {
        var envelope = CreateEnvelopeWithSlip(new RoutingSlipStep("UnknownStep"));

        var result = await _sut.ExecuteCurrentStepAsync(envelope);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.FailureReason, Does.Contain("No handler registered"));
    }

    [Test]
    public void ExecuteCurrentStepAsync_NoSlipInMetadata_ThrowsInvalidOperationException()
    {
        var envelope = new IntegrationEnvelope<string>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "Test",
            MessageType = "OrderCreated",
            Payload = "{}",
            Metadata = new Dictionary<string, string>(),
        };

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteCurrentStepAsync(envelope));
    }

    [Test]
    public void ExecuteCurrentStepAsync_EmptySlip_ThrowsInvalidOperationException()
    {
        var envelope = new IntegrationEnvelope<string>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "Test",
            MessageType = "OrderCreated",
            Payload = "{}",
            Metadata = new Dictionary<string, string>
            {
                [RoutingSlip.MetadataKey] = "[]",
            },
        };

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteCurrentStepAsync(envelope));
    }

    [Test]
    public void ExecuteCurrentStepAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.ExecuteCurrentStepAsync<string>(null!));
    }

    [Test]
    public async Task ExecuteCurrentStepAsync_WithParameters_PassesParametersToHandler()
    {
        IReadOnlyDictionary<string, string>? capturedParams = null;
        _transformHandler.HandleAsync<string>(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Do<IReadOnlyDictionary<string, string>?>(p => capturedParams = p),
            Arg.Any<CancellationToken>()).Returns(true);

        var parameters = new Dictionary<string, string> { ["format"] = "XML" };
        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Transform", null, parameters));

        await _sut.ExecuteCurrentStepAsync(envelope);

        Assert.That(capturedParams, Is.Not.Null);
        Assert.That(capturedParams!["format"], Is.EqualTo("XML"));
    }

    [Test]
    public async Task ExecuteCurrentStepAsync_UpdatesMetadataWithAdvancedSlip()
    {
        _validateHandler.HandleAsync<string>(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>()).Returns(true);

        var envelope = CreateEnvelopeWithSlip(
            new RoutingSlipStep("Validate"),
            new RoutingSlipStep("Transform"),
            new RoutingSlipStep("Deliver"));

        await _sut.ExecuteCurrentStepAsync(envelope);

        // After execution, the metadata should contain the advanced slip (Validate consumed)
        var updatedSlipJson = envelope.Metadata[RoutingSlip.MetadataKey];
        var updatedSteps = JsonSerializer.Deserialize<List<RoutingSlipStep>>(updatedSlipJson, JsonOptions);

        Assert.That(updatedSteps, Has.Count.EqualTo(2));
        Assert.That(updatedSteps![0].StepName, Is.EqualTo("Transform"));
        Assert.That(updatedSteps[1].StepName, Is.EqualTo("Deliver"));
    }
}
