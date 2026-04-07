# Tutorial 04 — The Integration Envelope

Deep dive into every `IntegrationEnvelope<T>` property: identity, expiration, metadata headers, sequence numbers, and immutable record semantics.

## Learning Objectives

After completing this tutorial you will be able to:

1. Use C# record `with` expressions to create modified envelopes without mutating the original
2. Create `FaultEnvelope` records from failed messages for dead-letter routing and replay
3. Track processing steps using `MessageHistoryEntry` for full audit trails
4. Verify that `ExpiresAt`, `ReplyTo`, and split-sequence fields survive channel delivery
5. Attach and read well-known `MessageHeaders` constants through real NATS
6. Construct a complex payload envelope with every field populated end-to-end

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

---

## Lab — Guided Practice

> **Purpose:** Run each test in order to see how record immutability, fault envelopes,
> message history, and every envelope field work end-to-end through real NATS JetStream.

| # | Test | Concept |
|---|------|---------|
| 1 | `Envelope_WithExpression_CreatesNewInstanceOriginalUnchanged` | Record `with` — immutable copy with overrides |
| 2 | `FaultEnvelope_CreateFromFailedMessage_PreservesCorrelation` | FaultEnvelope factory preserves identity |
| 3 | `FaultEnvelope_WithException_CapturesErrorDetails` | Exception type and message captured |
| 4 | `MessageHistoryEntry_RecordsProcessingSteps` | Message History audit trail pattern |
| 5 | `Envelope_ExpiresAt_SurvivedChannelDelivery` | ExpiresAt preserved through real NATS |
| 6 | `Envelope_ReplyTo_RequestReplyPatternThroughChannel` | ReplyTo (Return Address) through channel |
| 7 | `Envelope_SplitSequence_ThroughChannel` | SequenceNumber + TotalCount through channel |
| 8 | `Envelope_MetadataHeaders_WellKnownConstants` | MessageHeaders constants through real NATS |
| 9 | `Envelope_AllFields_ComplexPayloadThroughChannel` | Every field on a complex payload end-to-end |

> 💻 [`tests/TutorialLabs/Tutorial04/Lab.cs`](../tests/TutorialLabs/Tutorial04/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial04.Lab"
```

---

## Exam — Assessment Challenges

> **Purpose:** Prove you can apply envelope patterns in realistic integration
> scenarios — fault handling, multi-hop causation, and split-sequence reassembly.

| Difficulty | Challenge | What you prove |
|------------|-----------|---------------|
| 🟢 Starter | `Starter_FaultEnvelope_RetryExhaustion` | FaultEnvelope lifecycle with exception capture and retry exhaustion |
| 🟡 Intermediate | `Intermediate_CausationChain_ThreeHopsThroughChannel` | Multi-hop causation chain (Command → Event → Document) through channel |
| 🔴 Advanced | `Advanced_SplitSequence_AllPartsWithMetadataPreserved` | Split-sequence reassembly with full metadata verification |

> 💻 [`tests/TutorialLabs/Tutorial04/Exam.cs`](../tests/TutorialLabs/Tutorial04/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial04.Exam"
```

---

**Previous: [← Tutorial 03 — Your First Message](03-first-message.md)** | **Next: [Tutorial 05 — Message Brokers →](05-message-brokers.md)**
