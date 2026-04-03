using System.Text.Json;
using EnterpriseIntegrationPlatform.Activities;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Demo.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class PipelineOrchestratorTests
{
    private ITemporalWorkflowDispatcher _dispatcher = null!;
    private PipelineOptions _options = null!;
    private PipelineOrchestrator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dispatcher = Substitute.For<ITemporalWorkflowDispatcher>();
        _options = new PipelineOptions();
        _sut = new PipelineOrchestrator(
            _dispatcher,
            Options.Create(_options),
            NullLogger<PipelineOrchestrator>.Instance);
    }

    private static IntegrationEnvelope<JsonElement> BuildEnvelope(
        string payloadJson = """{"orderId":1}""")
    {
        var payload = JsonDocument.Parse(payloadJson).RootElement;
        return IntegrationEnvelope<JsonElement>.Create(
            payload,
            source: "TestSource",
            messageType: "OrderCreated");
    }

    [Test]
    public async Task ProcessAsync_ValidMessage_DispatchesToTemporal()
    {
        var envelope = BuildEnvelope();
        _dispatcher.DispatchAsync(Arg.Any<IntegrationPipelineInput>(), Arg.Any<string>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await _sut.ProcessAsync(envelope);

        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<IntegrationPipelineInput>(i => i.MessageId == envelope.MessageId),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_ValidMessage_SetsCorrectWorkflowId()
    {
        var envelope = BuildEnvelope();
        string? capturedWorkflowId = null;

        _dispatcher.DispatchAsync(
                Arg.Any<IntegrationPipelineInput>(),
                Arg.Do<string>(id => capturedWorkflowId = id),
                Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await _sut.ProcessAsync(envelope);

        Assert.That(capturedWorkflowId, Is.EqualTo($"integration-{envelope.MessageId}"));
    }

    [Test]
    public async Task ProcessAsync_ValidMessage_PassesCorrectInput()
    {
        var envelope = BuildEnvelope("""{"orderId":42}""");
        IntegrationPipelineInput? capturedInput = null;

        _dispatcher.DispatchAsync(
                Arg.Do<IntegrationPipelineInput>(i => capturedInput = i),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await _sut.ProcessAsync(envelope);

        Assert.That(capturedInput, Is.Not.Null);
        Assert.That(capturedInput!.MessageId, Is.EqualTo(envelope.MessageId));
        Assert.That(capturedInput.CorrelationId, Is.EqualTo(envelope.CorrelationId));
        Assert.That(capturedInput.Source, Is.EqualTo(envelope.Source));
        Assert.That(capturedInput.MessageType, Is.EqualTo(envelope.MessageType));
        Assert.That(capturedInput.PayloadJson, Does.Contain("42"));
        Assert.That(capturedInput.AckSubject, Is.EqualTo(_options.AckSubject));
        Assert.That(capturedInput.NackSubject, Is.EqualTo(_options.NackSubject));
    }

    [Test]
    public async Task ProcessAsync_ValidMessage_PassesPriorityAsInt()
    {
        var envelope = BuildEnvelope();
        IntegrationPipelineInput? capturedInput = null;

        _dispatcher.DispatchAsync(
                Arg.Do<IntegrationPipelineInput>(i => capturedInput = i),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await _sut.ProcessAsync(envelope);

        Assert.That(capturedInput!.Priority, Is.EqualTo((int)envelope.Priority));
    }

    [Test]
    public async Task ProcessAsync_ValidMessage_PassesSchemaVersion()
    {
        var envelope = BuildEnvelope();
        IntegrationPipelineInput? capturedInput = null;

        _dispatcher.DispatchAsync(
                Arg.Do<IntegrationPipelineInput>(i => capturedInput = i),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await _sut.ProcessAsync(envelope);

        Assert.That(capturedInput!.SchemaVersion, Is.EqualTo(envelope.SchemaVersion));
    }

    [Test]
    public async Task ProcessAsync_FailedMessage_DoesNotThrow()
    {
        var envelope = BuildEnvelope();
        _dispatcher.DispatchAsync(Arg.Any<IntegrationPipelineInput>(), Arg.Any<string>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, false, "Bad payload"));

        // Thin dispatcher does not throw on failure — it logs the result
        Assert.DoesNotThrowAsync(() => _sut.ProcessAsync(envelope));
    }

    [Test]
    public async Task ProcessAsync_DispatcherThrows_PropagatesException()
    {
        var envelope = BuildEnvelope();
        _dispatcher.DispatchAsync(Arg.Any<IntegrationPipelineInput>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("Temporal unavailable"));

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ProcessAsync(envelope));
    }

    [Test]
    public async Task ProcessAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _sut.ProcessAsync(null!));
    }

    [Test]
    public async Task ProcessAsync_EmptyMetadata_SetsMetadataJsonNull()
    {
        var envelope = BuildEnvelope();
        IntegrationPipelineInput? capturedInput = null;

        _dispatcher.DispatchAsync(
                Arg.Do<IntegrationPipelineInput>(i => capturedInput = i),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await _sut.ProcessAsync(envelope);

        Assert.That(capturedInput!.MetadataJson, Is.Null);
    }

    [Test]
    public async Task ProcessAsync_PassesCancellationToken()
    {
        var envelope = BuildEnvelope();
        using var cts = new CancellationTokenSource();

        _dispatcher.DispatchAsync(
                Arg.Any<IntegrationPipelineInput>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new IntegrationPipelineResult(envelope.MessageId, true));

        await _sut.ProcessAsync(envelope, cts.Token);

        await _dispatcher.Received(1).DispatchAsync(
            Arg.Any<IntegrationPipelineInput>(),
            Arg.Any<string>(),
            cts.Token);
    }
}
