using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

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
[TestFixture]
public class MessageLifecycleEndToEndTests 
{
    private IContainer? _lokiContainer;
    private LokiObservabilityEventLog? _lokiLog;
    private HttpClient? _httpClient;
    private bool _dockerAvailable;

    [SetUp]
    public async Task SetUp()
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

    [TearDown]
    public async Task TearDown()
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

    [Test]
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
        Assert.That(byCorrelation, Has.Count.EqualTo(4), "all four lifecycle stages should be persisted in Loki");

        Assert.That(byCorrelation[0].Stage, Is.EqualTo(MessageTracer.StageIngestion));
        Assert.That(byCorrelation[0].Status, Is.EqualTo(DeliveryStatus.Pending));

        Assert.That(byCorrelation[1].Stage, Is.EqualTo(MessageTracer.StageRouting));
        Assert.That(byCorrelation[1].Status, Is.EqualTo(DeliveryStatus.InFlight));

        Assert.That(byCorrelation[2].Stage, Is.EqualTo(MessageTracer.StageTransformation));
        Assert.That(byCorrelation[2].Status, Is.EqualTo(DeliveryStatus.InFlight));

        Assert.That(byCorrelation[3].Stage, Is.EqualTo(MessageTracer.StageDelivery));
        Assert.That(byCorrelation[3].Status, Is.EqualTo(DeliveryStatus.Delivered));

        // ── Query by business key ────────────────────────────────────────────
        var byBusinessKey = await _lokiLog.GetByBusinessKeyAsync(businessKey);
        Assert.That(byBusinessKey, Has.Count.EqualTo(4), "all events share the same business key");
        Assert.That(byBusinessKey, Has.All.Matches<MessageEvent>(e => e.CorrelationId == correlationId));
    }

    [Test]
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
        Assert.That(events, Has.Count.EqualTo(5), "ingestion + routing + failure + retry + delivery");

        Assert.That(events[0].Status, Is.EqualTo(DeliveryStatus.Pending));
        Assert.That(events[1].Status, Is.EqualTo(DeliveryStatus.InFlight));
        Assert.That(events[2].Status, Is.EqualTo(DeliveryStatus.Failed));
        Assert.That(events[2].Details, Does.Contain("Connection refused"));
        Assert.That(events[3].Status, Is.EqualTo(DeliveryStatus.Retrying));
        Assert.That(events[3].Details, Does.Contain("#1"));
        Assert.That(events[4].Status, Is.EqualTo(DeliveryStatus.Delivered));
    }

    [Test]
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
        Assert.That(events, Has.Count.EqualTo(5));

        Assert.That(events[^1].Status, Is.EqualTo(DeliveryStatus.DeadLettered));
        Assert.That(events[^1].Details, Does.Contain("Max retries exceeded"));

        // Also queryable by business key
        var byBk = await _lokiLog.GetByBusinessKeyAsync(businessKey);
        Assert.That(byBk, Has.Count.EqualTo(5));
    }

    [Test]
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

        Assert.That(first, Has.Count.EqualTo(2));
        Assert.That(second, Has.Count.EqualTo(2));
        Assert.That(third, Has.Count.EqualTo(2));

        // Data is identical across queries
        Assert.That(first.Select(e => e.Stage), Is.EquivalentTo(second.Select(e => e.Stage)));
    }

    [Test]
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
        Assert.That(result.Found, Is.True, "the message lifecycle was recorded in Loki");
        Assert.That(result.LatestStatus, Is.EqualTo(DeliveryStatus.Delivered));
        Assert.That(result.LatestStage, Is.EqualTo(MessageTracer.StageDelivery));
        Assert.That(result.Events, Has.Count.EqualTo(4));
        Assert.That(result.OllamaAvailable, Is.True);
        Assert.That(result.Summary, Does.Contain("delivered"));

        // Verify AI was called with the correct correlation ID and event data
        await traceAnalyzer.Received(1)
            .WhereIsMessageAsync(correlationId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
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
        Assert.That(result.Found, Is.True);
        Assert.That(result.LatestStatus, Is.EqualTo(DeliveryStatus.Delivered));
        Assert.That(result.LatestStage, Is.EqualTo(MessageTracer.StageDelivery));
        Assert.That(result.Events, Has.Count.EqualTo(3));
        Assert.That(result.Summary, Does.Contain("delivered"));
    }
}
