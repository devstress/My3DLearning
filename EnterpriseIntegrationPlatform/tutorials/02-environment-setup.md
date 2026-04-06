# Tutorial 02 — Setting Up Your Environment

Verify your .NET 10 environment by confirming that all core platform types, enums, and namespaces are present and correctly structured.

## Key Types

```csharp
// src/Contracts/IntegrationEnvelope.cs
public record IntegrationEnvelope<T> { /* ... */ }

// src/Contracts/MessagePriority.cs
public enum MessagePriority { Low = 0, Normal = 1, High = 2, Critical = 3 }

// src/Contracts/MessageIntent.cs
public enum MessageIntent { Command = 0, Document = 1, Event = 2 }

// src/Contracts/MessageHeaders.cs
public static class MessageHeaders
{
    public const string TraceId = "trace-id";
    public const string ContentType = "content-type";
    public const string SourceTopic = "source-topic";
    // ... 13 well-known header keys
}

// src/Ingestion/IMessageBrokerProducer.cs
public interface IMessageBrokerProducer { /* ... */ }

// src/Ingestion/IMessageBrokerConsumer.cs
public interface IMessageBrokerConsumer : IAsyncDisposable { /* ... */ }

// src/Ingestion/BrokerOptions.cs
public sealed class BrokerOptions
{
    public BrokerType BrokerType { get; set; } = BrokerType.NatsJetStream;
    public string ConnectionString { get; set; } = string.Empty;
    public int TransactionTimeoutSeconds { get; set; } = 30;
}

// src/Ingestion/BrokerType.cs
public enum BrokerType { NatsJetStream = 0, Kafka = 1, Pulsar = 2 }
```

## Exercises

### 1. Verify core types exist

```csharp
var envelopeType = typeof(IntegrationEnvelope<string>);
Assert.That(envelopeType, Is.Not.Null);
Assert.That(envelopeType.IsGenericType || envelopeType.IsClass, Is.True);

var producerType = typeof(IMessageBrokerProducer);
Assert.That(producerType.IsInterface, Is.True);

var consumerType = typeof(IMessageBrokerConsumer);
Assert.That(consumerType.IsInterface, Is.True);
Assert.That(typeof(IAsyncDisposable).IsAssignableFrom(consumerType), Is.True);
```

### 2. Verify BrokerType enum has exactly three values

```csharp
Assert.That(Enum.IsDefined(typeof(BrokerType), BrokerType.NatsJetStream), Is.True);
Assert.That(Enum.IsDefined(typeof(BrokerType), BrokerType.Kafka), Is.True);
Assert.That(Enum.IsDefined(typeof(BrokerType), BrokerType.Pulsar), Is.True);

var values = Enum.GetValues<BrokerType>();
Assert.That(values, Has.Length.EqualTo(3));
```

### 3. Verify MessagePriority ordinal values

```csharp
Assert.That((int)MessagePriority.Low, Is.EqualTo(0));
Assert.That((int)MessagePriority.Normal, Is.EqualTo(1));
Assert.That((int)MessagePriority.High, Is.EqualTo(2));
Assert.That((int)MessagePriority.Critical, Is.EqualTo(3));

var values = Enum.GetValues<MessagePriority>();
Assert.That(values, Has.Length.EqualTo(4));
```

### 4. Verify Contracts namespace contains expected types

```csharp
var assembly = typeof(IntegrationEnvelope<>).Assembly;
var typeNames = assembly.GetTypes()
    .Where(t => t.Namespace == "EnterpriseIntegrationPlatform.Contracts")
    .Select(t => t.Name)
    .ToList();

Assert.That(typeNames, Does.Contain("MessagePriority"));
Assert.That(typeNames, Does.Contain("MessageIntent"));
Assert.That(typeNames, Does.Contain("MessageHeaders"));
```

### 5. Verify Ingestion namespace contains expected types

```csharp
var assembly = typeof(IMessageBrokerProducer).Assembly;
var typeNames = assembly.GetTypes()
    .Where(t => t.Namespace == "EnterpriseIntegrationPlatform.Ingestion")
    .Select(t => t.Name)
    .ToList();

Assert.That(typeNames, Does.Contain("IMessageBrokerProducer"));
Assert.That(typeNames, Does.Contain("IMessageBrokerConsumer"));
Assert.That(typeNames, Does.Contain("BrokerOptions"));
Assert.That(typeNames, Does.Contain("BrokerType"));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial02/Lab.cs`](../tests/TutorialLabs/Tutorial02/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial02.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial02/Exam.cs`](../tests/TutorialLabs/Tutorial02/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial02.Exam"
```

---

**Previous: [← Tutorial 01 — Introduction](01-introduction.md)** | **Next: [Tutorial 03 — Your First Message →](03-first-message.md)**
