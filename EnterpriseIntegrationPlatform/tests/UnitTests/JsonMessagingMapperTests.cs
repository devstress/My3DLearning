using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace UnitTests;

/// <summary>
/// Tests for <see cref="JsonMessagingMapper{TDomain}"/> — the Messaging Mapper EIP pattern.
/// Covers domain→envelope mapping, envelope→domain extraction, null handling,
/// metadata preservation, child envelope correlation, and round-trip fidelity.
/// </summary>
[TestFixture]
public sealed class JsonMessagingMapperTests
{
    private ILogger<JsonMessagingMapper<OrderDto>> _logger = null!;
    private JsonMessagingMapper<OrderDto> _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<JsonMessagingMapper<OrderDto>>>();
        _sut = new JsonMessagingMapper<OrderDto>(_logger);
    }

    // ── ToEnvelope ──────────────────────────────────────────────────────────

    [Test]
    public void ToEnvelope_ValidDomain_ReturnsEnvelopeWithPayload()
    {
        var order = new OrderDto { OrderId = "ORD-001", Amount = 99.50m };

        var envelope = _sut.ToEnvelope(order, "OrderService", "order.created");

        Assert.That(envelope, Is.Not.Null);
        Assert.That(envelope.Payload, Is.SameAs(order));
        Assert.That(envelope.Source, Is.EqualTo("OrderService"));
        Assert.That(envelope.MessageType, Is.EqualTo("order.created"));
        Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(envelope.CorrelationId, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void ToEnvelope_SetsContentTypeMetadata()
    {
        var order = new OrderDto { OrderId = "ORD-002", Amount = 50m };

        var envelope = _sut.ToEnvelope(order, "OrderService", "order.created");

        Assert.That(envelope.Metadata.ContainsKey(MessageHeaders.ContentType), Is.True);
        Assert.That(envelope.Metadata[MessageHeaders.ContentType], Is.EqualTo("application/json"));
    }

    [Test]
    public void ToEnvelope_SetsClrTypeMetadata()
    {
        var order = new OrderDto { OrderId = "ORD-003", Amount = 10m };

        var envelope = _sut.ToEnvelope(order, "OrderService", "order.created");

        Assert.That(envelope.Metadata.ContainsKey("clr-type"), Is.True);
        Assert.That(envelope.Metadata["clr-type"], Does.Contain("OrderDto"));
    }

    [Test]
    public void ToEnvelope_WithCustomMetadata_PreservesAllMetadata()
    {
        var order = new OrderDto { OrderId = "ORD-004", Amount = 200m };
        var metadata = new Dictionary<string, string>
        {
            ["tenant-id"] = "tenant-42",
            ["region"] = "eu-west-1",
        };

        var envelope = _sut.ToEnvelope(order, "OrderService", "order.created", metadata);

        Assert.That(envelope.Metadata["tenant-id"], Is.EqualTo("tenant-42"));
        Assert.That(envelope.Metadata["region"], Is.EqualTo("eu-west-1"));
        // Also contains standard metadata
        Assert.That(envelope.Metadata[MessageHeaders.ContentType], Is.EqualTo("application/json"));
    }

    [Test]
    public void ToEnvelope_NullDomain_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _sut.ToEnvelope(null!, "OrderService", "order.created"));
    }

    [Test]
    public void ToEnvelope_NullSource_ThrowsArgumentException()
    {
        var order = new OrderDto { OrderId = "ORD-005", Amount = 1m };

        Assert.Throws<ArgumentNullException>(() =>
            _sut.ToEnvelope(order, null!, "order.created"));
    }

    [Test]
    public void ToEnvelope_EmptyMessageType_ThrowsArgumentException()
    {
        var order = new OrderDto { OrderId = "ORD-006", Amount = 1m };

        Assert.Throws<ArgumentException>(() =>
            _sut.ToEnvelope(order, "OrderService", ""));
    }

    // ── FromEnvelope ────────────────────────────────────────────────────────

    [Test]
    public void FromEnvelope_ValidEnvelope_ReturnsDomainObject()
    {
        var order = new OrderDto { OrderId = "ORD-010", Amount = 55m };
        var envelope = _sut.ToEnvelope(order, "OrderService", "order.created");

        var result = _sut.FromEnvelope(envelope);

        Assert.That(result, Is.SameAs(order));
        Assert.That(result.OrderId, Is.EqualTo("ORD-010"));
        Assert.That(result.Amount, Is.EqualTo(55m));
    }

    [Test]
    public void FromEnvelope_NullEnvelope_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _sut.FromEnvelope(null!));
    }

    [Test]
    public void FromEnvelope_NullPayload_ThrowsInvalidOperationException()
    {
        var envelope = new IntegrationEnvelope<OrderDto>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "test",
            MessageType = "test",
            Payload = null!,
        };

        Assert.Throws<InvalidOperationException>(() =>
            _sut.FromEnvelope(envelope));
    }

    // ── Round-trip ──────────────────────────────────────────────────────────

    [Test]
    public void RoundTrip_ToEnvelopeThenFromEnvelope_PreservesAllDomainData()
    {
        var original = new OrderDto { OrderId = "ORD-RT-001", Amount = 1234.56m };

        var envelope = _sut.ToEnvelope(original, "OrderService", "order.created");
        var extracted = _sut.FromEnvelope(envelope);

        Assert.That(extracted.OrderId, Is.EqualTo(original.OrderId));
        Assert.That(extracted.Amount, Is.EqualTo(original.Amount));
    }

    // ── ToChildEnvelope ─────────────────────────────────────────────────────

    [Test]
    public void ToChildEnvelope_PreservesCorrelationId()
    {
        var parentOrder = new OrderDto { OrderId = "ORD-P-001", Amount = 100m };
        var parentEnvelope = _sut.ToEnvelope(parentOrder, "OrderService", "order.created");

        var childOrder = new OrderDto { OrderId = "ORD-C-001", Amount = 25m };
        var childEnvelope = _sut.ToChildEnvelope(
            childOrder, parentEnvelope, "ShippingService", "order.split");

        Assert.That(childEnvelope.CorrelationId, Is.EqualTo(parentEnvelope.CorrelationId));
        Assert.That(childEnvelope.CausationId, Is.EqualTo(parentEnvelope.MessageId));
        Assert.That(childEnvelope.MessageId, Is.Not.EqualTo(parentEnvelope.MessageId));
    }

    [Test]
    public void ToChildEnvelope_InheritsParentMetadata()
    {
        var parentOrder = new OrderDto { OrderId = "ORD-P-002", Amount = 100m };
        var parentMetadata = new Dictionary<string, string>
        {
            ["tenant-id"] = "tenant-99",
        };
        var parentEnvelope = _sut.ToEnvelope(parentOrder, "OrderService", "order.created", parentMetadata);

        var childOrder = new OrderDto { OrderId = "ORD-C-002", Amount = 50m };
        var childEnvelope = _sut.ToChildEnvelope(
            childOrder, parentEnvelope, "PaymentService", "payment.initiated");

        Assert.That(childEnvelope.Metadata["tenant-id"], Is.EqualTo("tenant-99"));
    }

    [Test]
    public void ToChildEnvelope_NullParent_ThrowsArgumentNullException()
    {
        var childOrder = new OrderDto { OrderId = "ORD-C-003", Amount = 10m };

        Assert.Throws<ArgumentNullException>(() =>
            _sut.ToChildEnvelope<OrderDto>(
                childOrder, null!, "SomeService", "some.type"));
    }

    [Test]
    public void ToChildEnvelope_WithAdditionalMetadata_MergesMetadata()
    {
        var parentOrder = new OrderDto { OrderId = "ORD-P-003", Amount = 100m };
        var parentEnvelope = _sut.ToEnvelope(parentOrder, "OrderService", "order.created");

        var childOrder = new OrderDto { OrderId = "ORD-C-004", Amount = 75m };
        var childMetadata = new Dictionary<string, string>
        {
            ["priority"] = "high",
        };
        var childEnvelope = _sut.ToChildEnvelope(
            childOrder, parentEnvelope, "PaymentService", "payment.created", childMetadata);

        Assert.That(childEnvelope.Metadata["priority"], Is.EqualTo("high"));
        Assert.That(childEnvelope.Metadata[MessageHeaders.ContentType], Is.EqualTo("application/json"));
    }

    // ── Constructor validation ──────────────────────────────────────────────

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new JsonMessagingMapper<OrderDto>(null!));
    }

    [Test]
    public void Constructor_CustomSerializerOptions_AreUsed()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var mapper = new JsonMessagingMapper<OrderDto>(_logger, options);

        // Mapper should not throw — validates it accepts custom options
        var order = new OrderDto { OrderId = "ORD-CUSTOM", Amount = 1m };
        var envelope = mapper.ToEnvelope(order, "Test", "test.type");

        Assert.That(envelope.Payload.OrderId, Is.EqualTo("ORD-CUSTOM"));
    }

    // ── Test domain object ──────────────────────────────────────────────────

    /// <summary>
    /// Simple DTO used for testing the messaging mapper.
    /// </summary>
    public sealed class OrderDto
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
