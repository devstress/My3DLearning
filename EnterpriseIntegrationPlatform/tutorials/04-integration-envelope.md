# Tutorial 04 — The Integration Envelope

Deep dive into every `IntegrationEnvelope<T>` property: identity, expiration, metadata headers, sequence numbers, and immutable record semantics.

## Key Types

```csharp
// src/Contracts/IntegrationEnvelope.cs
public record IntegrationEnvelope<T>
{
    public Guid MessageId { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string Source { get; init; }
    public string MessageType { get; init; }
    public string SchemaVersion { get; init; } = "1.0";
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;
    public T Payload { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public string? ReplyTo { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public int? SequenceNumber { get; init; }
    public int? TotalCount { get; init; }
    public MessageIntent? Intent { get; init; }
    public bool IsExpired { get; }

    public static IntegrationEnvelope<T> Create(T payload, string source, string messageType,
        Guid? correlationId = null);
}

// src/Contracts/MessageHeaders.cs
public static class MessageHeaders
{
    public const string TraceId = "trace-id";
    public const string SpanId = "span-id";
    public const string ContentType = "content-type";
    public const string SourceTopic = "source-topic";
    public const string ConsumerGroup = "consumer-group";
    public const string RetryCount = "retry-count";
    public const string SequenceNumber = "sequence-number";
    public const string TotalCount = "total-count";
}
```

## Exercises

### 1. Set all properties on a complex payload envelope

```csharp
public sealed record ShipmentPayload(string ShipmentId, string Carrier, decimal WeightKg, string[] Items);

var items = new[] { "SKU-001", "SKU-002" };
var shipment = new ShipmentPayload("SHIP-1", "FedEx", 12.5m, items);
var correlationId = Guid.NewGuid();

var envelope = IntegrationEnvelope<ShipmentPayload>.Create(
    payload: shipment,
    source: "WarehouseService",
    messageType: "shipment.dispatched",
    correlationId: correlationId) with
{
    SchemaVersion = "2.0",
    Priority = MessagePriority.High,
    Intent = MessageIntent.Event,
    ReplyTo = "shipment-replies",
    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
    SequenceNumber = 0,
    TotalCount = 3,
};

Assert.That(envelope.Payload.ShipmentId, Is.EqualTo("SHIP-1"));
Assert.That(envelope.Payload.Carrier, Is.EqualTo("FedEx"));
Assert.That(envelope.Payload.WeightKg, Is.EqualTo(12.5m));
Assert.That(envelope.Payload.Items, Has.Length.EqualTo(2));
Assert.That(envelope.CorrelationId, Is.EqualTo(correlationId));
Assert.That(envelope.SchemaVersion, Is.EqualTo("2.0"));
Assert.That(envelope.Priority, Is.EqualTo(MessagePriority.High));
Assert.That(envelope.Intent, Is.EqualTo(MessageIntent.Event));
Assert.That(envelope.ReplyTo, Is.EqualTo("shipment-replies"));
Assert.That(envelope.ExpiresAt, Is.Not.Null);
Assert.That(envelope.SequenceNumber, Is.EqualTo(0));
Assert.That(envelope.TotalCount, Is.EqualTo(3));
```

### 2. Verify unique MessageId generation and independent CorrelationIds

```csharp
var ids = Enumerable.Range(0, 100)
    .Select(_ => IntegrationEnvelope<string>.Create("payload", "source", "type").MessageId)
    .ToList();

Assert.That(ids.Distinct().Count(), Is.EqualTo(100),
    "Each envelope must have a globally unique MessageId");

var env1 = IntegrationEnvelope<string>.Create("a", "src", "type");
var env2 = IntegrationEnvelope<string>.Create("b", "src", "type");
Assert.That(env1.CorrelationId, Is.Not.EqualTo(env2.CorrelationId));
```

### 3. Test IsExpired with past, future, and null ExpiresAt

```csharp
var expired = IntegrationEnvelope<string>.Create("stale", "source", "type") with
{
    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5),
};
Assert.That(expired.IsExpired, Is.True);

var fresh = IntegrationEnvelope<string>.Create("fresh", "source", "type") with
{
    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
};
Assert.That(fresh.IsExpired, Is.False);

var immortal = IntegrationEnvelope<string>.Create("immortal", "source", "type");
Assert.That(immortal.ExpiresAt, Is.Null);
Assert.That(immortal.IsExpired, Is.False);
```

### 4. Add and read metadata headers

```csharp
var envelope = IntegrationEnvelope<string>.Create("payload", "source", "type") with
{
    Metadata = new Dictionary<string, string>
    {
        [MessageHeaders.ContentType] = "application/json",
        [MessageHeaders.TraceId] = "abc-123-trace",
        [MessageHeaders.SourceTopic] = "orders-topic",
    },
};

Assert.That(envelope.Metadata[MessageHeaders.ContentType], Is.EqualTo("application/json"));
Assert.That(envelope.Metadata[MessageHeaders.TraceId], Is.EqualTo("abc-123-trace"));
Assert.That(envelope.Metadata[MessageHeaders.SourceTopic], Is.EqualTo("orders-topic"));
Assert.That(envelope.Metadata, Has.Count.EqualTo(3));
```

### 5. Model a Splitter output with sequence numbers

```csharp
var correlationId = Guid.NewGuid();
var parts = Enumerable.Range(0, 3)
    .Select(i => IntegrationEnvelope<string>.Create(
        payload: $"Part-{i}",
        source: "Splitter",
        messageType: "order.part",
        correlationId: correlationId) with
    {
        SequenceNumber = i,
        TotalCount = 3,
    })
    .ToList();

Assert.That(parts, Has.Count.EqualTo(3));

for (var i = 0; i < 3; i++)
{
    Assert.That(parts[i].SequenceNumber, Is.EqualTo(i));
    Assert.That(parts[i].TotalCount, Is.EqualTo(3));
    Assert.That(parts[i].CorrelationId, Is.EqualTo(correlationId));
}
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial04/Lab.cs`](../tests/TutorialLabs/Tutorial04/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial04.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial04/Exam.cs`](../tests/TutorialLabs/Tutorial04/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial04.Exam"
```

---

**Previous: [← Tutorial 03 — Your First Message](03-first-message.md)** | **Next: [Tutorial 05 — Message Brokers →](05-message-brokers.md)**
