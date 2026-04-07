# Tutorial 03 — Your First Message

Create an `IntegrationEnvelope<T>`, understand its anatomy (auto-generated identity, causation chains, priority, metadata, expiration), and deliver messages through Point-to-Point and Publish-Subscribe channels using MockEndpoint for verified end-to-end testing.

## Key Types

```csharp
// src/Contracts/IntegrationEnvelope.cs — canonical message wrapper
public record IntegrationEnvelope<T>
{
    public required Guid MessageId { get; init; }
    public required Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }              // parent→child lineage
    public required DateTimeOffset Timestamp { get; init; }
    public required string Source { get; init; }
    public required string MessageType { get; init; }
    public string SchemaVersion { get; init; } = "1.0";
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;
    public required T Payload { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public DateTimeOffset? ExpiresAt { get; init; }       // Message Expiration
    public int? SequenceNumber { get; init; }             // Splitter position
    public int? TotalCount { get; init; }
    public MessageIntent? Intent { get; init; }           // Command/Document/Event
    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;

    public static IntegrationEnvelope<T> Create(
        T payload, string source, string messageType,
        Guid? correlationId = null, Guid? causationId = null);
}

// src/Ingestion/Channels/PointToPointChannel.cs — queue semantics
public sealed class PointToPointChannel : IPointToPointChannel
{
    // Each message delivered to exactly one consumer in the group
    Task SendAsync<T>(IntegrationEnvelope<T> envelope, string channel, CancellationToken ct);
}

// src/Ingestion/Channels/PublishSubscribeChannel.cs — fan-out delivery
public sealed class PublishSubscribeChannel : IPublishSubscribeChannel
{
    // Every subscriber receives every message
    Task PublishAsync<T>(IntegrationEnvelope<T> envelope, string channel, CancellationToken ct);
}
```

## Exercises

### 1. Create an envelope and verify auto-generated identity fields

```csharp
var envelope = IntegrationEnvelope<string>.Create(
    "Hello, Messaging!", "Tutorial03", "greeting");

// MessageId, CorrelationId, Timestamp are auto-generated
Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));
Assert.That(envelope.CorrelationId, Is.Not.EqualTo(Guid.Empty));
Assert.That(envelope.Timestamp, Is.GreaterThan(DateTimeOffset.MinValue));
```

### 2. Build a causation chain (parent→child lineage)

```csharp
var parent = IntegrationEnvelope<string>.Create(
    "original-order", "OrderService", "order.created");

var child = IntegrationEnvelope<string>.Create(
    "order-validated", "ValidationService", "order.validated",
    correlationId: parent.CorrelationId,
    causationId: parent.MessageId);

// Child references parent; both share the same CorrelationId
Assert.That(child.CausationId, Is.EqualTo(parent.MessageId));
Assert.That(child.CorrelationId, Is.EqualTo(parent.CorrelationId));
```

### 3. Set priority, intent, and expiration

```csharp
var urgentCommand = IntegrationEnvelope<string>.Create(
    "shutdown-now", "OpsService", "infra.command") with
{
    Priority = MessagePriority.Critical,
    Intent = MessageIntent.Command,
    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
};

Assert.That(urgentCommand.Priority, Is.EqualTo(MessagePriority.Critical));
Assert.That(urgentCommand.IsExpired, Is.False);
```

### 4. Point-to-Point channel — queue delivery

```csharp
var output = new MockEndpoint("output");
var channel = new PointToPointChannel(
    output, output, NullLogger<PointToPointChannel>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "order-created", "OrderService", "order.created");

await channel.SendAsync(envelope, "orders-queue", CancellationToken.None);

output.AssertReceivedCount(1);
Assert.That(output.GetReceived<string>().Payload, Is.EqualTo("order-created"));
```

### 5. Publish-Subscribe channel — fan-out to multiple subscribers

```csharp
var sub1 = new MockEndpoint("subscriber-1");
var sub2 = new MockEndpoint("subscriber-2");

var ch1 = new PublishSubscribeChannel(
    sub1, sub1, NullLogger<PublishSubscribeChannel>.Instance);
var ch2 = new PublishSubscribeChannel(
    sub2, sub2, NullLogger<PublishSubscribeChannel>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "price-updated", "PricingService", "price.changed");

await ch1.PublishAsync(envelope, "price-events", CancellationToken.None);
await ch2.PublishAsync(envelope, "price-events", CancellationToken.None);

sub1.AssertReceivedCount(1);
sub2.AssertReceivedCount(1);
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial03/Lab.cs`](../tests/TutorialLabs/Tutorial03/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial03.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial03/Exam.cs`](../tests/TutorialLabs/Tutorial03/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial03.Exam"
```

---

**Previous: [← Tutorial 02 — Environment Setup](02-environment-setup.md)** | **Next: [Tutorial 04 — The Integration Envelope →](04-integration-envelope.md)**
