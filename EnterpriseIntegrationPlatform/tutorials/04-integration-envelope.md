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

## Lab

**Objective:** Build causation chains and sequenced message sets that demonstrate how the Envelope Wrapper pattern preserves **atomicity** and **traceability** across a multi-step integration pipeline.

### Step 1: Build a Causation Chain (Message Lineage)

Write code that simulates a three-step processing pipeline. Create an original envelope with `IntegrationEnvelope<string>.Create()`. Then create a second envelope (transformation result) using a `with` expression — set its `CausationId` to the first envelope's `MessageId` and keep the same `CorrelationId`. Create a third envelope whose `CausationId` is the second. Verify all three share the same `CorrelationId` but have distinct `MessageId` values.

This lineage is essential for **atomicity**: if step 3 fails, the saga compensation engine uses the `CausationId` chain to identify and roll back exactly the right upstream steps.

### Step 2: Model a Splitter Output with Sequencing

Create three envelopes representing a Splitter's output. Use `with` expressions to set `SequenceNumber` (0, 1, 2) and `TotalCount` (3). Also set `ExpiresAt` on one envelope to 5 minutes from now, and on another to a time in the past. Verify `IsExpired` returns the correct value.

Explain why the **Message Expiration** pattern is critical for scalability: in a high-throughput system, stale messages must be routed to the Dead Letter Queue rather than consuming resources processing outdated data.

### Step 3: Design an Atomicity Scenario

Imagine an order message is split into 3 line-item messages. Line-item 2 fails delivery. Using the envelope fields (`CorrelationId`, `CausationId`, `SequenceNumber`, `TotalCount`), describe how the platform can: (a) identify all 3 messages as belonging to the same operation, (b) determine which specific message failed, and (c) trigger compensation for line-items 1 and 3 that already succeeded.

## Exam

1. Why is `IntegrationEnvelope<T>` defined as a C# `record` rather than a `class`?
   - A) Records are faster to serialize than classes
   - B) Records provide immutability via `with` expressions, ensuring envelopes are never accidentally mutated during concurrent processing — critical for thread-safe scalability
   - C) The .NET runtime requires records for generic types
   - D) Records automatically encrypt their properties

2. In a causation chain where message A is split into messages B₁, B₂, and B₃, what value should the `CausationId` of each split message contain?
   - A) Its own `MessageId`
   - B) The `CorrelationId` of message A
   - C) The `MessageId` of message A — the parent that caused the split
   - D) A new randomly generated `Guid`

3. How does the `IsExpired` check contribute to the platform's **zero message loss** guarantee?
   - A) Expired messages are silently dropped to save resources
   - B) Expired messages are routed to the Dead Letter Queue with reason "expired", ensuring they are never silently lost but also don't consume processing capacity for stale data
   - C) The broker automatically deletes expired messages
   - D) `IsExpired` prevents messages from being published in the first place

---

**Previous: [← Tutorial 03 — Your First Message](03-first-message.md)** | **Next: [Tutorial 05 — Message Brokers →](05-message-brokers.md)**
