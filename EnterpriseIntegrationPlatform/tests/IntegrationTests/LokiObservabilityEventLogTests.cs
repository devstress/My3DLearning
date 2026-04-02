using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

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
[TestFixture]
public class LokiObservabilityEventLogTests
{
    private LokiObservabilityEventLog? _log;
    private HttpClient? _httpClient;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (!SharedLokiFixture.DockerAvailable) return;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(SharedLokiFixture.LokiBaseUrl + "/"),
            Timeout = TimeSpan.FromSeconds(30),
        };
        _log = new LokiObservabilityEventLog(
            _httpClient,
            NullLogger<LokiObservabilityEventLog>.Instance);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _httpClient?.Dispose();
    }

    private bool SkipIfNoDocker() => !SharedLokiFixture.DockerAvailable;

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

    [Test]
    public async Task RecordAsync_StoresEvent_GetByCorrelationIdReturnsIt()
    {
        if (SkipIfNoDocker()) return;

        var correlationId = Guid.NewGuid();
        var evt = CreateEvent(correlationId);

        await _log!.RecordAsync(evt);
        await SharedLokiFixture.WaitForLokiIndexAsync(
            async () => (await _log.GetByCorrelationIdAsync(correlationId)).Count, 1);

        var result = await _log.GetByCorrelationIdAsync(correlationId);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].CorrelationId, Is.EqualTo(correlationId));
        Assert.That(result[0].MessageType, Is.EqualTo("OrderShipment"));
        Assert.That(result[0].Stage, Is.EqualTo("Ingestion"));
    }

    [Test]
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
        await SharedLokiFixture.WaitForLokiIndexAsync(
            async () => (await _log.GetByBusinessKeyAsync(businessKey)).Count, 2);

        var result = await _log.GetByBusinessKeyAsync(businessKey);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Stage, Is.EqualTo("Ingestion"));
        Assert.That(result[1].Stage, Is.EqualTo("Routing"));
    }

    [Test]
    public async Task GetByBusinessKeyAsync_IsCaseInsensitive()
    {
        if (SkipIfNoDocker()) return;

        var correlationId = Guid.NewGuid();
        var businessKey = $"Order-CI-{Guid.NewGuid():N}";

        await _log!.RecordAsync(CreateEvent(correlationId, businessKey));
        await SharedLokiFixture.WaitForLokiIndexAsync(
            async () => (await _log.GetByBusinessKeyAsync(businessKey.ToUpperInvariant())).Count, 1);

        // Query with different case
        var result = await _log.GetByBusinessKeyAsync(businessKey.ToUpperInvariant());

        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetByBusinessKeyAsync_ReturnsEmpty_WhenNotFound()
    {
        if (SkipIfNoDocker()) return;

        var result = await _log!.GetByBusinessKeyAsync("nonexistent-key-" + Guid.NewGuid());

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetByCorrelationIdAsync_ReturnsEmpty_WhenNotFound()
    {
        if (SkipIfNoDocker()) return;

        var result = await _log!.GetByCorrelationIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task MultipleCorrelationIds_WithSameBusinessKey_ReturnsAllEvents()
    {
        if (SkipIfNoDocker()) return;

        var corr1 = Guid.NewGuid();
        var corr2 = Guid.NewGuid();
        var businessKey = $"order-multi-{Guid.NewGuid():N}";

        await _log!.RecordAsync(CreateEvent(corr1, businessKey, "Ingestion"));
        await _log.RecordAsync(CreateEvent(corr2, businessKey, "Ingestion"));
        await SharedLokiFixture.WaitForLokiIndexAsync(
            async () => (await _log.GetByBusinessKeyAsync(businessKey)).Count, 2);

        var result = await _log.GetByBusinessKeyAsync(businessKey);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
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
        await SharedLokiFixture.WaitForLokiIndexAsync(
            async () => (await _log.GetByCorrelationIdAsync(correlationId)).Count, 1);

        var result = await _log.GetByCorrelationIdAsync(correlationId);
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
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
        await SharedLokiFixture.WaitForLokiIndexAsync(
            async () => (await _log.GetByCorrelationIdAsync(correlationId)).Count, 2);

        var result = await _log.GetByCorrelationIdAsync(correlationId);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Stage, Is.EqualTo("Ingestion"));
        Assert.That(result[1].Stage, Is.EqualTo("Routing"));
    }
}
