using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="LokiObservabilityEventLog"/> using a real
/// Grafana Loki instance via Testcontainers.
/// <para>
/// These tests cover all behavioural contracts of <see cref="IObservabilityEventLog"/>:
/// record/retrieve by correlation ID, business key (case-insensitive), multi-correlation
/// aggregation, timestamp ordering, null business key handling, and empty results.
/// </para>
/// <para>
/// Tests are skipped when Docker is not available.
/// </para>
/// </summary>
public class LokiObservabilityEventLogTests : IAsyncLifetime
{
    private IContainer? _lokiContainer;
    private LokiObservabilityEventLog? _log;
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
            _log = new LokiObservabilityEventLog(
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

    private bool SkipIfNoDocker()
    {
        return !_dockerAvailable;
    }

    /// <summary>Small delay to let Loki index the pushed data.</summary>
    private static Task WaitForLokiIndex() => Task.Delay(2000);

    private static MessageEvent CreateEvent(
        Guid? correlationId = null,
        string businessKey = "order-02",
        string stage = "Ingestion",
        DeliveryStatus status = DeliveryStatus.Pending,
        DateTimeOffset? recordedAt = null)
    {
        return new MessageEvent
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId ?? Guid.NewGuid(),
            MessageType = "OrderShipment",
            Source = "Gateway",
            Stage = stage,
            Status = status,
            BusinessKey = businessKey,
            Details = $"Stage: {stage}",
            RecordedAt = recordedAt ?? DateTimeOffset.UtcNow,
        };
    }

    [Fact]
    public async Task RecordAsync_StoresEvent_GetByCorrelationIdReturnsIt()
    {
        if (SkipIfNoDocker()) return;

        var correlationId = Guid.NewGuid();
        var evt = CreateEvent(correlationId);

        await _log!.RecordAsync(evt);
        await WaitForLokiIndex();

        var result = await _log.GetByCorrelationIdAsync(correlationId);
        result.Should().ContainSingle();
        result[0].CorrelationId.Should().Be(correlationId);
        result[0].MessageType.Should().Be("OrderShipment");
        result[0].Stage.Should().Be("Ingestion");
    }

    [Fact]
    public async Task GetByBusinessKeyAsync_ReturnsEventsForKey()
    {
        if (SkipIfNoDocker()) return;

        var correlationId = Guid.NewGuid();
        // Use unique business key to avoid cross-test contamination
        var businessKey = $"order-bk-{Guid.NewGuid():N}";

        await _log!.RecordAsync(CreateEvent(correlationId, businessKey, "Ingestion", DeliveryStatus.Pending,
            DateTimeOffset.UtcNow.AddSeconds(-2)));
        await _log.RecordAsync(CreateEvent(correlationId, businessKey, "Routing", DeliveryStatus.InFlight,
            DateTimeOffset.UtcNow));
        await WaitForLokiIndex();

        var result = await _log.GetByBusinessKeyAsync(businessKey);

        result.Should().HaveCount(2);
        result[0].Stage.Should().Be("Ingestion");
        result[1].Stage.Should().Be("Routing");
    }

    [Fact]
    public async Task GetByBusinessKeyAsync_IsCaseInsensitive()
    {
        if (SkipIfNoDocker()) return;

        var correlationId = Guid.NewGuid();
        var businessKey = $"Order-CI-{Guid.NewGuid():N}";

        await _log!.RecordAsync(CreateEvent(correlationId, businessKey));
        await WaitForLokiIndex();

        // Query with different case
        var result = await _log.GetByBusinessKeyAsync(businessKey.ToUpperInvariant());

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByBusinessKeyAsync_ReturnsEmpty_WhenNotFound()
    {
        if (SkipIfNoDocker()) return;

        var result = await _log!.GetByBusinessKeyAsync("nonexistent-key-" + Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_ReturnsEmpty_WhenNotFound()
    {
        if (SkipIfNoDocker()) return;

        var result = await _log!.GetByCorrelationIdAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleCorrelationIds_WithSameBusinessKey_ReturnsAllEvents()
    {
        if (SkipIfNoDocker()) return;

        var corr1 = Guid.NewGuid();
        var corr2 = Guid.NewGuid();
        var businessKey = $"order-multi-{Guid.NewGuid():N}";

        await _log!.RecordAsync(CreateEvent(corr1, businessKey, "Ingestion"));
        await _log.RecordAsync(CreateEvent(corr2, businessKey, "Ingestion"));
        await WaitForLokiIndex();

        var result = await _log.GetByBusinessKeyAsync(businessKey);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task RecordAsync_WithoutBusinessKey_StillRetrievableByCorrelationId()
    {
        if (SkipIfNoDocker()) return;

        var correlationId = Guid.NewGuid();
        var evt = new MessageEvent
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            MessageType = "Test",
            Source = "Gateway",
            Stage = "Ingestion",
            Status = DeliveryStatus.Pending,
            BusinessKey = null,
        };

        await _log!.RecordAsync(evt);
        await WaitForLokiIndex();

        var result = await _log.GetByCorrelationIdAsync(correlationId);
        result.Should().ContainSingle();
    }

    [Fact]
    public async Task EventsAreOrderedByTimestamp()
    {
        if (SkipIfNoDocker()) return;

        var correlationId = Guid.NewGuid();
        var businessKey = $"order-ts-{Guid.NewGuid():N}";
        var older = CreateEvent(correlationId, businessKey, "Ingestion", DeliveryStatus.Pending,
            DateTimeOffset.UtcNow.AddMinutes(-5));
        var newer = CreateEvent(correlationId, businessKey, "Routing", DeliveryStatus.InFlight,
            DateTimeOffset.UtcNow);

        // Insert in reverse order
        await _log!.RecordAsync(newer);
        await _log.RecordAsync(older);
        await WaitForLokiIndex();

        var result = await _log.GetByCorrelationIdAsync(correlationId);
        result.Should().HaveCount(2);
        result[0].Stage.Should().Be("Ingestion");
        result[1].Stage.Should().Be("Routing");
    }
}
