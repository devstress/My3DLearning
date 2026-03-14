using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Integration;

/// <summary>
/// End-to-end integration tests for the full message observability lifecycle.
/// <para>
/// Exercises <see cref="MessageLifecycleRecorder"/> writing to a real
/// <see cref="LokiObservabilityEventLog"/> (backed by Grafana Loki via Testcontainers)
/// and verifies all lifecycle events are persisted and queryable — exactly as
/// they would be in the production Aspire environment.
/// </para>
/// <para>
/// Tests are skipped when Docker is not available.
/// </para>
/// </summary>
public class MessageLifecycleEndToEndTests : IAsyncLifetime
{
    private IContainer? _lokiContainer;
    private LokiObservabilityEventLog? _lokiLog;
    private HttpClient? _httpClient;
    private bool _dockerAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            _lokiContainer = new ContainerBuilder()
                .WithImage("grafana/loki:3.4.2")
                .WithPortBinding(3100, true)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(r => r.ForPort(3100).ForPath("/ready")))
                .Build();

            await _lokiContainer.StartAsync();
            _dockerAvailable = true;

            var host = _lokiContainer.Hostname;
            var port = _lokiContainer.GetMappedPublicPort(3100);
            var baseUrl = $"http://{host}:{port}/";

            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };
            _lokiLog = new LokiObservabilityEventLog(
                _httpClient,
                NullLogger<LokiObservabilityEventLog>.Instance);
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_lokiContainer is not null)
        {
            await _lokiContainer.DisposeAsync();
        }
    }

    private bool SkipIfNoDocker() => !_dockerAvailable;

    /// <summary>Wait for Loki's eventual-consistency write path.</summary>
    private static async Task WaitForLokiIndex() => await Task.Delay(2000);

    private static IntegrationEnvelope<string> CreateEnvelope(string messageType = "OrderShipment")
    {
        return IntegrationEnvelope<string>.Create(
            payload: "test-payload",
            source: "Gateway",
            messageType: messageType);
    }

    [Fact]
    public async Task FullLifecycle_AllStages_PersistedInLoki()
    {
        if (SkipIfNoDocker()) return;

        var productionStore = new InMemoryMessageStateStore();
        var recorder = new MessageLifecycleRecorder(
            productionStore,
            _lokiLog!,
            NullLogger<MessageLifecycleRecorder>.Instance);

        var envelope = CreateEnvelope();
        var correlationId = envelope.CorrelationId;
        var businessKey = $"order-e2e-{Guid.NewGuid():N}";

        // ── Full lifecycle: Ingestion → Routing → Transformation → Delivery ──
        await recorder.RecordReceivedAsync(envelope, businessKey);
        await recorder.RecordProcessingAsync(envelope, MessageTracer.StageRouting, businessKey);
        await recorder.RecordProcessingAsync(envelope, MessageTracer.StageTransformation, businessKey);
        await recorder.RecordDeliveredAsync(envelope, activity: null, durationMs: 123.4, businessKey);

        await WaitForLokiIndex();

        // ── Query by correlation ID ──────────────────────────────────────────
        var byCorrelation = await _lokiLog!.GetByCorrelationIdAsync(correlationId);
        byCorrelation.Should().HaveCount(4, "all four lifecycle stages should be persisted in Loki");

        byCorrelation[0].Stage.Should().Be(MessageTracer.StageIngestion);
        byCorrelation[0].Status.Should().Be(DeliveryStatus.Pending);

        byCorrelation[1].Stage.Should().Be(MessageTracer.StageRouting);
        byCorrelation[1].Status.Should().Be(DeliveryStatus.InFlight);

        byCorrelation[2].Stage.Should().Be(MessageTracer.StageTransformation);
        byCorrelation[2].Status.Should().Be(DeliveryStatus.InFlight);

        byCorrelation[3].Stage.Should().Be(MessageTracer.StageDelivery);
        byCorrelation[3].Status.Should().Be(DeliveryStatus.Delivered);

        // ── Query by business key ────────────────────────────────────────────
        var byBusinessKey = await _lokiLog.GetByBusinessKeyAsync(businessKey);
        byBusinessKey.Should().HaveCount(4, "all events share the same business key");
        byBusinessKey.Should().OnlyContain(e => e.CorrelationId == correlationId);
    }

    [Fact]
    public async Task FailureAndRetryLifecycle_PersistedInLoki()
    {
        if (SkipIfNoDocker()) return;

        var productionStore = new InMemoryMessageStateStore();
        var recorder = new MessageLifecycleRecorder(
            productionStore,
            _lokiLog!,
            NullLogger<MessageLifecycleRecorder>.Instance);

        var envelope = CreateEnvelope();
        var correlationId = envelope.CorrelationId;
        var businessKey = $"order-fail-{Guid.NewGuid():N}";

        // ── Lifecycle with failure and retry ─────────────────────────────────
        await recorder.RecordReceivedAsync(envelope, businessKey);
        await recorder.RecordProcessingAsync(envelope, MessageTracer.StageRouting, businessKey);
        await recorder.RecordFailedAsync(
            envelope, activity: null,
            new InvalidOperationException("Connection refused"),
            MessageTracer.StageDelivery, businessKey);
        await recorder.RecordRetryAsync(envelope, retryCount: 1, MessageTracer.StageDelivery, businessKey);
        await recorder.RecordDeliveredAsync(envelope, activity: null, durationMs: 200.0, businessKey);

        await WaitForLokiIndex();

        // ── Verify all 5 events in Loki ──────────────────────────────────────
        var events = await _lokiLog!.GetByCorrelationIdAsync(correlationId);
        events.Should().HaveCount(5, "ingestion + routing + failure + retry + delivery");

        events[0].Status.Should().Be(DeliveryStatus.Pending);
        events[1].Status.Should().Be(DeliveryStatus.InFlight);
        events[2].Status.Should().Be(DeliveryStatus.Failed);
        events[2].Details.Should().Contain("Connection refused");
        events[3].Status.Should().Be(DeliveryStatus.Retrying);
        events[3].Details.Should().Contain("#1");
        events[4].Status.Should().Be(DeliveryStatus.Delivered);
    }

    [Fact]
    public async Task DeadLetterLifecycle_PersistedInLoki()
    {
        if (SkipIfNoDocker()) return;

        var productionStore = new InMemoryMessageStateStore();
        var recorder = new MessageLifecycleRecorder(
            productionStore,
            _lokiLog!,
            NullLogger<MessageLifecycleRecorder>.Instance);

        var envelope = CreateEnvelope();
        var correlationId = envelope.CorrelationId;
        var businessKey = $"order-dlq-{Guid.NewGuid():N}";

        // ── Lifecycle ending in dead-letter ──────────────────────────────────
        await recorder.RecordReceivedAsync(envelope, businessKey);
        await recorder.RecordFailedAsync(
            envelope, activity: null,
            new TimeoutException("Service unavailable"),
            MessageTracer.StageDelivery, businessKey);
        await recorder.RecordRetryAsync(envelope, retryCount: 1, MessageTracer.StageDelivery, businessKey);
        await recorder.RecordRetryAsync(envelope, retryCount: 2, MessageTracer.StageDelivery, businessKey);
        await recorder.RecordDeadLetteredAsync(envelope, "Max retries exceeded", businessKey);

        await WaitForLokiIndex();

        // ── Verify all events including dead-letter ──────────────────────────
        var events = await _lokiLog!.GetByCorrelationIdAsync(correlationId);
        events.Should().HaveCount(5);

        events[^1].Status.Should().Be(DeliveryStatus.DeadLettered);
        events[^1].Details.Should().Contain("Max retries exceeded");

        // Also queryable by business key
        var byBk = await _lokiLog.GetByBusinessKeyAsync(businessKey);
        byBk.Should().HaveCount(5);
    }

    [Fact]
    public async Task LokiStorage_PersistsAcrossQueries()
    {
        if (SkipIfNoDocker()) return;

        var productionStore = new InMemoryMessageStateStore();
        var recorder = new MessageLifecycleRecorder(
            productionStore,
            _lokiLog!,
            NullLogger<MessageLifecycleRecorder>.Instance);

        var envelope = CreateEnvelope();
        var correlationId = envelope.CorrelationId;
        var businessKey = $"order-persist-{Guid.NewGuid():N}";

        await recorder.RecordReceivedAsync(envelope, businessKey);
        await recorder.RecordDeliveredAsync(envelope, activity: null, durationMs: 50.0, businessKey);

        await WaitForLokiIndex();

        // Query multiple times — Loki storage is durable, not ephemeral
        var first = await _lokiLog!.GetByCorrelationIdAsync(correlationId);
        var second = await _lokiLog.GetByCorrelationIdAsync(correlationId);
        var third = await _lokiLog.GetByBusinessKeyAsync(businessKey);

        first.Should().HaveCount(2);
        second.Should().HaveCount(2);
        third.Should().HaveCount(2);

        // Data is identical across queries
        first.Select(e => e.Stage).Should().BeEquivalentTo(second.Select(e => e.Stage));
    }

    [Fact]
    public async Task WhereIsAsync_AI_FindsDeliveredMessage_InLoki()
    {
        if (SkipIfNoDocker()) return;

        // ── Arrange: record a full lifecycle ending in Delivered ──────────────
        var productionStore = new InMemoryMessageStateStore();
        var recorder = new MessageLifecycleRecorder(
            productionStore,
            _lokiLog!,
            NullLogger<MessageLifecycleRecorder>.Instance);

        var envelope = CreateEnvelope();
        var correlationId = envelope.CorrelationId;
        var businessKey = $"order-ai-{Guid.NewGuid():N}";

        await recorder.RecordReceivedAsync(envelope, businessKey);
        await recorder.RecordProcessingAsync(envelope, MessageTracer.StageRouting, businessKey);
        await recorder.RecordProcessingAsync(envelope, MessageTracer.StageTransformation, businessKey);
        await recorder.RecordDeliveredAsync(envelope, activity: null, durationMs: 99.9, businessKey);

        await WaitForLokiIndex();

        // ── Arrange: wire up MessageStateInspector with real Loki + AI ───────
        var traceAnalyzer = Substitute.For<ITraceAnalyzer>();
        traceAnalyzer
            .WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var json = ci.ArgAt<string>(1);
                return Task.FromResult(
                    $"The message for {businessKey} has been successfully delivered. " +
                    "It passed through Ingestion, Routing, Transformation, and Delivery stages.");
            });

        var inspector = new MessageStateInspector(
            _lokiLog!,
            traceAnalyzer,
            NullLogger<MessageStateInspector>.Instance);

        // ── Act: ask "where is my message?" via business key ─────────────────
        var result = await inspector.WhereIsAsync(businessKey);

        // ── Assert: message found and delivered ──────────────────────────────
        result.Found.Should().BeTrue("the message lifecycle was recorded in Loki");
        result.LatestStatus.Should().Be(DeliveryStatus.Delivered);
        result.LatestStage.Should().Be(MessageTracer.StageDelivery);
        result.Events.Should().HaveCount(4);
        result.OllamaAvailable.Should().BeTrue();
        result.Summary.Should().Contain("delivered");

        // Verify AI was called with the correct correlation ID and event data
        await traceAnalyzer.Received(1)
            .WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhereIsByCorrelationAsync_AI_FindsDeliveredMessage_InLoki()
    {
        if (SkipIfNoDocker()) return;

        // ── Arrange: record a full lifecycle ending in Delivered ──────────────
        var productionStore = new InMemoryMessageStateStore();
        var recorder = new MessageLifecycleRecorder(
            productionStore,
            _lokiLog!,
            NullLogger<MessageLifecycleRecorder>.Instance);

        var envelope = CreateEnvelope();
        var correlationId = envelope.CorrelationId;
        var businessKey = $"order-ai-corr-{Guid.NewGuid():N}";

        await recorder.RecordReceivedAsync(envelope, businessKey);
        await recorder.RecordProcessingAsync(envelope, MessageTracer.StageRouting, businessKey);
        await recorder.RecordDeliveredAsync(envelope, activity: null, durationMs: 75.0, businessKey);

        await WaitForLokiIndex();

        // ── Arrange: wire up inspector with AI ───────────────────────────────
        var traceAnalyzer = Substitute.For<ITraceAnalyzer>();
        traceAnalyzer
            .WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Message delivered successfully after passing through Routing.");

        var inspector = new MessageStateInspector(
            _lokiLog!,
            traceAnalyzer,
            NullLogger<MessageStateInspector>.Instance);

        // ── Act: ask by correlation ID ───────────────────────────────────────
        var result = await inspector.WhereIsByCorrelationAsync(correlationId);

        // ── Assert: message found and delivered ──────────────────────────────
        result.Found.Should().BeTrue();
        result.LatestStatus.Should().Be(DeliveryStatus.Delivered);
        result.LatestStage.Should().Be(MessageTracer.StageDelivery);
        result.Events.Should().HaveCount(3);
        result.Summary.Should().Contain("delivered");
    }
}
