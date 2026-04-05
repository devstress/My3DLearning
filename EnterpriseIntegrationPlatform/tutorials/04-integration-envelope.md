# Tutorial 04 — The Integration Envelope

## What You'll Learn

- Every field of `IntegrationEnvelope<T>` and when to use it
- Message headers and metadata conventions
- How the envelope implements multiple EIP patterns
- Envelope immutability and the causation chain

---

## The Canonical Message Format

The `IntegrationEnvelope<T>` is the single most important type in the platform. Every message — whether it's an HTTP request body, an SFTP file, an email, or an internal event — is wrapped in this envelope before entering the processing pipeline.

```
┌─────────────────────────────────────────────────────┐
│                IntegrationEnvelope<T>                │
│                                                     │
│  ┌─── Identity ──────────────────────────────────┐  │
│  │ MessageId      : Guid (unique per message)    │  │
│  │ CorrelationId  : Guid (links related msgs)    │  │
│  │ CausationId    : Guid? (parent message)       │  │
│  └───────────────────────────────────────────────┘  │
│                                                     │
│  ┌─── Source & Type ─────────────────────────────┐  │
│  │ Source         : string ("order-system")      │  │
│  │ MessageType    : string ("OrderCreated")      │  │
│  │ SchemaVersion  : string (default "1.0")       │  │
│  │ Timestamp      : DateTimeOffset               │  │
│  └───────────────────────────────────────────────┘  │
│                                                     │
│  ┌─── Payload ───────────────────────────────────┐  │
│  │ Payload        : T (the actual data)          │  │
│  └───────────────────────────────────────────────┘  │
│                                                     │
│  ┌─── Quality of Service ────────────────────────┐  │
│  │ Priority       : Low | Normal | High | Crit   │  │
│  │ ReplyTo        : string? (return address)     │  │
│  │ ExpiresAt      : DateTimeOffset? (TTL)        │  │
│  └───────────────────────────────────────────────┘  │
│                                                     │
│  ┌─── Sequencing ────────────────────────────────┐  │
│  │ SequenceNumber : int? (position in batch)     │  │
│  │ TotalCount     : int? (total in batch)        │  │
│  └───────────────────────────────────────────────┘  │
│                                                     │
│  ┌─── Classification ───────────────────────────┐  │
│  │ Intent         : Command | Document | Event?  │  │
│  └───────────────────────────────────────────────┘  │
│                                                     │
│  ┌─── Metadata ──────────────────────────────────┐  │
│  │ Dictionary<string, string> (extensible headers)│  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

---

## Field-by-Field Deep Dive

### Identity Fields

**MessageId** — Every message gets a unique `Guid`. This is used for:
- Idempotent processing (deduplication in Cassandra)
- Audit trail
- DLQ tracking

**CorrelationId** — Links all messages that belong to the same logical operation. When a message is split into parts, all parts share the same `CorrelationId`. When a request gets a reply, both carry the same `CorrelationId`.

**CausationId** — Points to the `MessageId` of the message that caused this one. This builds a causal chain:

```
Original Order (MessageId: A, CausationId: null)
  └─ Validated Order (MessageId: B, CausationId: A)
       └─ Transformed Order (MessageId: C, CausationId: B)
            └─ Delivery Confirmation (MessageId: D, CausationId: C)
```

### Source and Type

**Source** — Identifies the originating system. Examples: `"order-system"`, `"crm"`, `"partner-api"`.

**MessageType** — A string discriminator for the payload type. Routing rules often match on this field.

**SchemaVersion** — Version of the message contract schema (default: `"1.0"`). Allows consumers to handle schema evolution.

**Timestamp** — When the message was created (UTC). Used for ordering, expiration checks, and audit.

### Quality of Service

**Priority** — Determines processing order when queues have backlog:

| Priority | Use Case |
|----------|----------|
| `Critical` | System alerts, circuit breaker notifications |
| `High` | Financial transactions, SLA-bound deliveries |
| `Normal` | Standard business messages (default) |
| `Low` | Analytics, batch reports, non-urgent |

**ReplyTo** — Implements the **Return Address** EIP pattern. Tells the receiver where to send the response.

**ExpiresAt** — Implements the **Message Expiration** EIP pattern. The `MessageExpirationChecker` in `Processing.DeadLetter` routes expired messages to the DLQ.

### Sequencing

**SequenceNumber** and **TotalCount** — Used by the **Splitter** and **Resequencer** patterns:

```
Original batch (5 items) → Splitter produces:
  Item 1: SequenceNumber=1, TotalCount=5, CorrelationId=X
  Item 2: SequenceNumber=2, TotalCount=5, CorrelationId=X
  Item 3: SequenceNumber=3, TotalCount=5, CorrelationId=X
  Item 4: SequenceNumber=4, TotalCount=5, CorrelationId=X
  Item 5: SequenceNumber=5, TotalCount=5, CorrelationId=X
```

The **Aggregator** uses `TotalCount` to know when all parts have arrived.

### Message Intent

**Intent** (`MessageIntent?`, nullable) — Implements three EIP **Message Construction** patterns:

| Intent | EIP Pattern | Meaning | Example |
|--------|------------|---------|---------|
| `Command` | Command Message | "Do this" — an instruction to perform an action | "ProcessPayment" |
| `Document` | Document Message | "Here's data" — information transfer with no expected action | "QuarterlyReport" |
| `Event` | Event Message | "This happened" — notification of something that occurred | "OrderCreated" |

### Metadata

The `Metadata` dictionary carries extensible key-value headers. The `MessageHeaders` static class defines well-known keys:

```csharp
public static class MessageHeaders
{
    public const string TraceId = "trace-id";
    public const string SpanId = "span-id";
    public const string ContentType = "content-type";
    public const string SourceTopic = "source-topic";
    public const string ConsumerGroup = "consumer-group";
    public const string RetryCount = "retry-count";
    public const string SequenceNumber = "sequence-number";
    // ... and more
}
```

---

## EIP Patterns Implemented by the Envelope

The envelope alone implements **9 EIP patterns**:

| EIP Pattern | Envelope Feature |
|-------------|-----------------|
| **Message** | The envelope IS the message |
| **Envelope Wrapper** | Wraps raw payload with standardized metadata |
| **Correlation Identifier** | `CorrelationId` field |
| **Return Address** | `ReplyTo` field |
| **Message Expiration** | `ExpiresAt` field |
| **Message Sequence** | `SequenceNumber` + `TotalCount` fields |
| **Format Indicator** | `MessageHeaders.ContentType` in metadata |
| **Command Message** | `Intent = Command` |
| **Document Message** | `Intent = Document` |
| **Event Message** | `Intent = Event` |

---

## Envelope Immutability

`IntegrationEnvelope<T>` is a C# `record`, which means it's **immutable by default**. When a processing step needs to modify the envelope (e.g., add metadata, change the payload), it creates a **new** envelope using `with`:

```csharp
// Original envelope
var original = new IntegrationEnvelope<string> { /* ... */ };

// Create a new envelope with updated metadata
var enriched = original with
{
    Metadata = new Dictionary<string, string>(original.Metadata)
    {
        ["enriched-at"] = DateTimeOffset.UtcNow.ToString("O")
    }
};

// The original is unchanged
// enriched has the new metadata entry
```

This immutability is critical for:
- **Thread safety** — Multiple activities can read the same envelope concurrently
- **Audit trail** — Each step produces a new envelope; the original is preserved
- **Replay** — You can always replay the original, unmodified envelope

---

## The Causation Chain

As a message flows through the pipeline, each step creates a new envelope with `CausationId` set to the previous step's `MessageId`. This creates a traceable chain:

```
Ingress → Validate → Transform → Route → Deliver

Envelope A (Ingress):    MessageId=aaa, CausationId=null
Envelope B (Validate):   MessageId=bbb, CausationId=aaa
Envelope C (Transform):  MessageId=ccc, CausationId=bbb
Envelope D (Route):      MessageId=ddd, CausationId=ccc
Envelope E (Deliver):    MessageId=eee, CausationId=ddd
```

All five envelopes share the same `CorrelationId`. This lets you:
- Query "show me all messages for this order" (by CorrelationId)
- Query "what caused this message?" (by CausationId)
- Reconstruct the full processing history

---

## Exercises

1. **Design an envelope**: You receive an XML invoice from a partner via SFTP. Design the `IntegrationEnvelope` fields — what would `Source`, `MessageType`, `Intent`, and key metadata entries be?

2. **Trace the chain**: A message arrives, gets validated, split into 3 parts, each part is transformed, and all 3 are aggregated. How many envelopes are created total? Draw the `CausationId` chain.

3. **Expiration scenario**: A message has `ExpiresAt = now + 5 minutes`. Processing takes 6 minutes. What happens? Which component handles this?

---

**Previous: [← Tutorial 03 — Your First Message](03-first-message.md)** | **Next: [Tutorial 05 — Message Brokers →](05-message-brokers.md)**
