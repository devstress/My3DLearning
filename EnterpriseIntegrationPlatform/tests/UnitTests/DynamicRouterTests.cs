using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class DynamicRouterTests
{
    private IMessageBrokerProducer _producer = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
    }

    private DynamicRouter BuildRouter(DynamicRouterOptions? options = null) =>
        new(
            _producer,
            Options.Create(options ?? new DynamicRouterOptions()),
            NullLogger<DynamicRouter>.Instance);

    private static IntegrationEnvelope<JsonElement> BuildEnvelope(
        string messageType = "OrderCreated",
        string source = "OrderService",
        MessagePriority priority = MessagePriority.Normal,
        string payloadJson = """{"orderId":1}""",
        Dictionary<string, string>? metadata = null)
    {
        var payload = JsonDocument.Parse(payloadJson).RootElement;
        var envelope = IntegrationEnvelope<JsonElement>.Create(
            payload,
            source: source,
            messageType: messageType);

        return metadata is null
            ? envelope with { Priority = priority }
            : envelope with { Priority = priority, Metadata = metadata };
    }

    // ------------------------------------------------------------------ //
    // Registration
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RegisterAsync_NewConditionKey_AddsEntryToRoutingTable()
    {
        var sut = BuildRouter();

        await sut.RegisterAsync("OrderCreated", "orders.created", "participant-1");

        var table = sut.GetRoutingTable();
        Assert.That(table, Has.Count.EqualTo(1));
        Assert.That(table.ContainsKey("OrderCreated"), Is.True);
        Assert.That(table["OrderCreated"].Destination, Is.EqualTo("orders.created"));
        Assert.That(table["OrderCreated"].ParticipantId, Is.EqualTo("participant-1"));
    }

    [Test]
    public async Task RegisterAsync_DuplicateConditionKey_ReplacesExistingEntry()
    {
        var sut = BuildRouter();

        await sut.RegisterAsync("OrderCreated", "orders.v1", "participant-1");
        await sut.RegisterAsync("OrderCreated", "orders.v2", "participant-2");

        var table = sut.GetRoutingTable();
        Assert.That(table, Has.Count.EqualTo(1));
        Assert.That(table["OrderCreated"].Destination, Is.EqualTo("orders.v2"));
        Assert.That(table["OrderCreated"].ParticipantId, Is.EqualTo("participant-2"));
    }

    [Test]
    public async Task RegisterAsync_MultipleConditionKeys_AddsAllEntries()
    {
        var sut = BuildRouter();

        await sut.RegisterAsync("OrderCreated", "orders.created");
        await sut.RegisterAsync("InvoiceCreated", "invoices.created");
        await sut.RegisterAsync("PaymentReceived", "payments.received");

        var table = sut.GetRoutingTable();
        Assert.That(table, Has.Count.EqualTo(3));
    }

    [Test]
    public void RegisterAsync_NullConditionKey_ThrowsArgumentNullException()
    {
        var sut = BuildRouter();

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await sut.RegisterAsync(null!, "some-destination"));
    }

    [Test]
    public void RegisterAsync_EmptyDestination_ThrowsArgumentException()
    {
        var sut = BuildRouter();

        Assert.ThrowsAsync<ArgumentException>(
            async () => await sut.RegisterAsync("OrderCreated", ""));
    }

    // ------------------------------------------------------------------ //
    // Unregistration
    // ------------------------------------------------------------------ //

    [Test]
    public async Task UnregisterAsync_ExistingConditionKey_RemovesEntryAndReturnsTrue()
    {
        var sut = BuildRouter();
        await sut.RegisterAsync("OrderCreated", "orders.created");

        var result = await sut.UnregisterAsync("OrderCreated");

        Assert.That(result, Is.True);
        Assert.That(sut.GetRoutingTable(), Has.Count.EqualTo(0));
    }

    [Test]
    public async Task UnregisterAsync_NonExistentConditionKey_ReturnsFalse()
    {
        var sut = BuildRouter();

        var result = await sut.UnregisterAsync("DoesNotExist");

        Assert.That(result, Is.False);
    }

    // ------------------------------------------------------------------ //
    // Routing to dynamic destination
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_MatchingDynamicRoute_PublishesToRegisteredDestination()
    {
        var sut = BuildRouter();
        await sut.RegisterAsync("OrderCreated", "orders.dynamic");

        var envelope = BuildEnvelope(messageType: "OrderCreated");
        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("orders.dynamic"));
        Assert.That(decision.IsFallback, Is.False);
        Assert.That(decision.MatchedEntry, Is.Not.Null);
        Assert.That(decision.MatchedEntry!.Destination, Is.EqualTo("orders.dynamic"));
        Assert.That(decision.ConditionValue, Is.EqualTo("OrderCreated"));

        await _producer.Received(1).PublishAsync(
            envelope, "orders.dynamic", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RouteAsync_CaseInsensitiveMatch_RoutesToRegisteredDestination()
    {
        var sut = BuildRouter(new DynamicRouterOptions { CaseInsensitive = true });
        await sut.RegisterAsync("ordercreated", "orders.dynamic");

        var envelope = BuildEnvelope(messageType: "OrderCreated");
        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("orders.dynamic"));
        Assert.That(decision.IsFallback, Is.False);
    }

    [Test]
    public async Task RouteAsync_CaseSensitiveNoMatch_UsesFallback()
    {
        var options = new DynamicRouterOptions
        {
            CaseInsensitive = false,
            FallbackTopic = "fallback.topic",
        };
        var sut = BuildRouter(options);
        await sut.RegisterAsync("ordercreated", "orders.dynamic");

        var envelope = BuildEnvelope(messageType: "OrderCreated");
        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.IsFallback, Is.True);
        Assert.That(decision.Destination, Is.EqualTo("fallback.topic"));
    }

    [Test]
    public async Task RouteAsync_AfterUnregister_UsesFallback()
    {
        var options = new DynamicRouterOptions { FallbackTopic = "fallback.topic" };
        var sut = BuildRouter(options);
        await sut.RegisterAsync("OrderCreated", "orders.dynamic");
        await sut.UnregisterAsync("OrderCreated");

        var envelope = BuildEnvelope(messageType: "OrderCreated");
        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.IsFallback, Is.True);
        Assert.That(decision.Destination, Is.EqualTo("fallback.topic"));
    }

    // ------------------------------------------------------------------ //
    // Fallback on unknown
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_NoMatchWithFallback_UsesFallbackTopic()
    {
        var options = new DynamicRouterOptions { FallbackTopic = "integration.fallback" };
        var sut = BuildRouter(options);

        var envelope = BuildEnvelope(messageType: "UnknownType");
        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("integration.fallback"));
        Assert.That(decision.IsFallback, Is.True);
        Assert.That(decision.MatchedEntry, Is.Null);

        await _producer.Received(1).PublishAsync(
            envelope, "integration.fallback", Arg.Any<CancellationToken>());
    }

    [Test]
    public void RouteAsync_NoMatchAndNoFallback_ThrowsInvalidOperationException()
    {
        var sut = BuildRouter(new DynamicRouterOptions { FallbackTopic = null });
        var envelope = BuildEnvelope(messageType: "UnknownType");

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.RouteAsync(envelope));
    }

    [Test]
    public void RouteAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var sut = BuildRouter();

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await sut.RouteAsync<string>(null!));
    }

    // ------------------------------------------------------------------ //
    // Condition field: Source
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_ConditionFieldSource_RoutesBasedOnSource()
    {
        var options = new DynamicRouterOptions { ConditionField = "Source" };
        var sut = BuildRouter(options);
        await sut.RegisterAsync("PaymentService", "payments.dynamic");

        var envelope = BuildEnvelope(source: "PaymentService");
        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("payments.dynamic"));
        Assert.That(decision.ConditionValue, Is.EqualTo("PaymentService"));
    }

    // ------------------------------------------------------------------ //
    // Condition field: Metadata
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_ConditionFieldMetadata_RoutesBasedOnMetadataValue()
    {
        var options = new DynamicRouterOptions { ConditionField = "Metadata.region" };
        var sut = BuildRouter(options);
        await sut.RegisterAsync("eu-west", "eu.topic");

        var envelope = BuildEnvelope(metadata: new Dictionary<string, string> { ["region"] = "eu-west" });
        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("eu.topic"));
    }

    [Test]
    public async Task RouteAsync_ConditionFieldMetadataMissing_UsesFallback()
    {
        var options = new DynamicRouterOptions
        {
            ConditionField = "Metadata.region",
            FallbackTopic = "fallback.topic",
        };
        var sut = BuildRouter(options);
        await sut.RegisterAsync("eu-west", "eu.topic");

        var envelope = BuildEnvelope(); // no metadata
        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.IsFallback, Is.True);
        Assert.That(decision.ConditionValue, Is.Null);
    }

    // ------------------------------------------------------------------ //
    // Condition field: Priority
    // ------------------------------------------------------------------ //

    [Test]
    public async Task RouteAsync_ConditionFieldPriority_RoutesBasedOnPriority()
    {
        var options = new DynamicRouterOptions { ConditionField = "Priority" };
        var sut = BuildRouter(options);
        await sut.RegisterAsync("High", "high-priority.topic");

        var envelope = BuildEnvelope(priority: MessagePriority.High);
        var decision = await sut.RouteAsync(envelope);

        Assert.That(decision.Destination, Is.EqualTo("high-priority.topic"));
    }

    // ------------------------------------------------------------------ //
    // GetRoutingTable returns a snapshot
    // ------------------------------------------------------------------ //

    [Test]
    public async Task GetRoutingTable_ReturnsSnapshotNotLiveReference()
    {
        var sut = BuildRouter();
        await sut.RegisterAsync("OrderCreated", "orders.created");

        var snapshot = sut.GetRoutingTable();

        await sut.RegisterAsync("InvoiceCreated", "invoices.created");

        Assert.That(snapshot, Has.Count.EqualTo(1));
        Assert.That(sut.GetRoutingTable(), Has.Count.EqualTo(2));
    }
}
