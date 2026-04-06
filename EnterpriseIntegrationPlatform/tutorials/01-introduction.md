# Tutorial 01 — Introduction to Enterprise Integration

Enterprise integration connects applications through messaging. This platform implements 65+ EIP patterns in .NET 10.

## Key Types

```csharp
// The universal message wrapper — every message in the platform is an IntegrationEnvelope<T>
// src/Contracts/IntegrationEnvelope.cs
public record IntegrationEnvelope<T>
{
    public Guid MessageId { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public string Source { get; init; }
    public string MessageType { get; init; }
    public T Payload { get; init; }
    public MessagePriority Priority { get; init; }
    public string SchemaVersion { get; init; }
    public MessageIntent? Intent { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
}

// Publish and consume via broker abstractions
// src/Ingestion/IMessageBrokerProducer.cs
public interface IMessageBrokerProducer
{
    Task PublishAsync<T>(IntegrationEnvelope<T> envelope, string topic, CancellationToken ct = default);
}

// src/Ingestion/IMessageBrokerConsumer.cs
public interface IMessageBrokerConsumer : IAsyncDisposable
{
    Task SubscribeAsync<T>(string topic, Func<IntegrationEnvelope<T>, Task> handler, CancellationToken ct = default);
}
```

## Exercises

### 1. Create an envelope and verify auto-generated fields

```csharp
var envelope = IntegrationEnvelope<string>.Create(
    payload: "Hello, EIP!",
    source: "Tutorial01",
    messageType: "greeting.created");

Assert.That(envelope.MessageId, Is.Not.EqualTo(Guid.Empty));
Assert.That(envelope.CorrelationId, Is.Not.EqualTo(Guid.Empty));
Assert.That(envelope.Timestamp, Is.Not.EqualTo(default(DateTimeOffset)));
Assert.That(envelope.Source, Is.EqualTo("Tutorial01"));
Assert.That(envelope.Payload, Is.EqualTo("Hello, EIP!"));
```

### 2. Check default values on a new envelope

```csharp
var envelope = IntegrationEnvelope<string>.Create("payload", "source", "type");

Assert.That(envelope.SchemaVersion, Is.EqualTo("1.0"));
Assert.That(envelope.Priority, Is.EqualTo(MessagePriority.Normal));
Assert.That(envelope.CausationId, Is.Null);
Assert.That(envelope.ReplyTo, Is.Null);
Assert.That(envelope.Metadata, Is.Empty);
```

### 3. Set message intent using `with` expression

```csharp
var command = IntegrationEnvelope<string>.Create(
    "PlaceOrder", "OrderService", "order.place") with
{
    Intent = MessageIntent.Command,
};

Assert.That(command.Intent, Is.EqualTo(MessageIntent.Command));
```

### 4. Verify platform types exist (EIP pattern mapping)

```csharp
// EIP: Message Channel → IMessageBrokerProducer
var producerType = typeof(IMessageBrokerProducer);
Assert.That(producerType.IsInterface, Is.True);
Assert.That(producerType.GetMethod("PublishAsync"), Is.Not.Null);

// EIP: Message Endpoint → IMessageBrokerConsumer
var consumerType = typeof(IMessageBrokerConsumer);
Assert.That(consumerType.IsInterface, Is.True);
Assert.That(consumerType.GetMethod("SubscribeAsync"), Is.Not.Null);
```

### 5. Verify IntegrationEnvelope is a C# record with value equality

```csharp
var envelopeType = typeof(IntegrationEnvelope<string>);
Assert.That(envelopeType.IsClass, Is.True);

var equatable = typeof(IEquatable<IntegrationEnvelope<string>>);
Assert.That(equatable.IsAssignableFrom(envelopeType), Is.True);
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial01/Lab.cs`](../tests/TutorialLabs/Tutorial01/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial01.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial01/Exam.cs`](../tests/TutorialLabs/Tutorial01/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial01.Exam"
```

---

**Next: [Tutorial 02 — Setting Up Your Environment →](02-environment-setup.md)**
