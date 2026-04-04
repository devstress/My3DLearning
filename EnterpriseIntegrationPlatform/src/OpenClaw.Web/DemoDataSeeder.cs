using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Observability;

namespace OpenClaw.Web;

/// <summary>
/// Background service that seeds the observability event log with sample
/// message lifecycle events so operators can test "where is my message?"
/// queries in OpenClaw even before the Kafka ingestion pipeline is running.
/// <para>
/// This ensures that when Aspire starts, OpenClaw already has data to show
/// for demo business keys like <c>order-02</c>, <c>shipment-123</c>, etc.
/// </para>
/// </summary>
public sealed class DemoDataSeeder : BackgroundService
{
    private readonly IObservabilityEventLog _observabilityLog;
    private readonly ILogger<DemoDataSeeder> _logger;

    /// <summary>
    /// Gets a value indicating whether demo data has been fully seeded.
    /// Used by the <c>/api/health/seeder</c> endpoint to allow Playwright
    /// tests to poll for readiness before querying seeded data.
    /// </summary>
    public static bool IsSeeded { get; private set; }

    public DemoDataSeeder(
        IObservabilityEventLog observabilityLog,
        ILogger<DemoDataSeeder> logger)
    {
        _observabilityLog = observabilityLog;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Seeding demo observability data for OpenClaw…");

        // ── order-02: successfully delivered ──────────────────────────────────
        var orderCorrelation = Guid.NewGuid();
        var orderMessageId = Guid.NewGuid();
        var baseTime = DateTimeOffset.UtcNow.AddMinutes(-10);

        await SeedEventAsync(orderMessageId, orderCorrelation, "OrderShipment", "Gateway",
            "Ingestion", DeliveryStatus.Pending, "order-02", "Message received from gateway",
            baseTime, stoppingToken);

        await SeedEventAsync(orderMessageId, orderCorrelation, "OrderShipment", "Router",
            "Routing", DeliveryStatus.InFlight, "order-02", "Routed to shipment fulfilment pipeline",
            baseTime.AddSeconds(2), stoppingToken);

        await SeedEventAsync(orderMessageId, orderCorrelation, "OrderShipment", "Transformer",
            "Transformation", DeliveryStatus.InFlight, "order-02", "Transformed to canonical format",
            baseTime.AddSeconds(5), stoppingToken);

        await SeedEventAsync(orderMessageId, orderCorrelation, "OrderShipment", "Delivery",
            "Delivery", DeliveryStatus.Delivered, "order-02", "Delivered successfully in 150.3ms",
            baseTime.AddSeconds(8), stoppingToken);

        // ── shipment-123: currently in-flight ─────────────────────────────────
        var shipmentCorrelation = Guid.NewGuid();
        var shipmentMessageId = Guid.NewGuid();

        await SeedEventAsync(shipmentMessageId, shipmentCorrelation, "ShipmentTracking", "Gateway",
            "Ingestion", DeliveryStatus.Pending, "shipment-123", "Shipment tracking event received",
            baseTime, stoppingToken);

        await SeedEventAsync(shipmentMessageId, shipmentCorrelation, "ShipmentTracking", "Router",
            "Routing", DeliveryStatus.InFlight, "shipment-123", "Routing to tracking handler",
            baseTime.AddSeconds(3), stoppingToken);

        // ── invoice-001: failed with retry ────────────────────────────────────
        var invoiceCorrelation = Guid.NewGuid();
        var invoiceMessageId = Guid.NewGuid();

        await SeedEventAsync(invoiceMessageId, invoiceCorrelation, "InvoicePayment", "Billing",
            "Ingestion", DeliveryStatus.Pending, "invoice-001", "Invoice payment event received",
            baseTime, stoppingToken);

        await SeedEventAsync(invoiceMessageId, invoiceCorrelation, "InvoicePayment", "Transformer",
            "Transformation", DeliveryStatus.InFlight, "invoice-001", "Transforming payment data",
            baseTime.AddSeconds(2), stoppingToken);

        await SeedEventAsync(invoiceMessageId, invoiceCorrelation, "InvoicePayment", "Delivery",
            "Delivery", DeliveryStatus.Failed, "invoice-001", "Failed: Connection refused to payment gateway",
            baseTime.AddSeconds(5), stoppingToken);

        await SeedEventAsync(invoiceMessageId, invoiceCorrelation, "InvoicePayment", "Delivery",
            "Delivery", DeliveryStatus.Retrying, "invoice-001", "Retry attempt #1",
            baseTime.AddSeconds(10), stoppingToken);

        _logger.LogInformation(
            "Demo data seeded: order-02 (delivered), shipment-123 (in-flight), invoice-001 (retrying)");

        IsSeeded = true;
    }

    private async Task SeedEventAsync(
        Guid messageId, Guid correlationId, string messageType, string source,
        string stage, DeliveryStatus status, string businessKey, string details,
        DateTimeOffset recordedAt, CancellationToken ct)
    {
        var evt = new MessageEvent
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            MessageType = messageType,
            Source = source,
            Stage = stage,
            Status = status,
            BusinessKey = businessKey,
            Details = details,
            RecordedAt = recordedAt,
        };

        await _observabilityLog.RecordAsync(evt, ct);
    }
}
