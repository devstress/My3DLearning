# Tutorial 11 — Dynamic Router

## What You'll Learn

- The EIP Dynamic Router pattern and how it differs from static routing
- How `IDynamicRouter` resolves destinations from a runtime routing table
- How `IRouterControlChannel` lets downstream participants register/unregister
- The `DynamicRoutingDecision` with fallback handling
- How the routing table is built at runtime, not at deployment time

---

## EIP Pattern: Dynamic Router

> *"Use a Dynamic Router, a Router that can self-configure based on special configuration messages from participating destinations."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  Participant A ──register("typeA","topic-a")──▶ ┌──────────────────┐
  Participant B ──register("typeB","topic-b")──▶ │ Control Channel   │
  Participant C ──unregister("typeC")──────────▶ │                  │
                                                  └────────┬─────────┘
                                                           │ updates
                                                           ▼
                                                  ┌──────────────────┐
                              ──Message──▶        │  Dynamic Router  │──▶ topic-a / topic-b / fallback
                                                  └──────────────────┘
```

Unlike the Content-Based Router whose rules are fixed at startup, the Dynamic Router's routing table is **learned at runtime** as downstream participants register and unregister through the control channel.

---

## Platform Implementation

### IDynamicRouter

```csharp
// src/Processing.Routing/IDynamicRouter.cs
public interface IDynamicRouter
{
    Task<DynamicRoutingDecision> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
```

### IRouterControlChannel

```csharp
// src/Processing.Routing/IRouterControlChannel.cs
public interface IRouterControlChannel
{
    Task RegisterAsync(
        string conditionKey,
        string destination,
        string? participantId = null,
        CancellationToken cancellationToken = default);

    Task<bool> UnregisterAsync(
        string conditionKey,
        CancellationToken cancellationToken = default);

    IReadOnlyDictionary<string, DynamicRouteEntry> GetRoutingTable();
}
```

Downstream services call `RegisterAsync` on startup with their condition key and destination topic. The router looks up the condition field from the envelope, finds the matching entry, and publishes the message.

### DynamicRoutingDecision

```csharp
// src/Processing.Routing/DynamicRoutingDecision.cs
public sealed record DynamicRoutingDecision(
    string Destination,
    DynamicRouteEntry? MatchedEntry,
    bool IsFallback,
    string? ConditionValue);
```

### DynamicRouteEntry

```csharp
public sealed record DynamicRouteEntry(
    string ConditionKey,
    string Destination,
    string? ParticipantId,
    DateTimeOffset RegisteredAtUtc);
```

When no routing table entry matches, the router uses the configured fallback topic (`IsFallback = true`). If no fallback is configured, it throws `InvalidOperationException`.

---

## Scalability Dimension

The routing table is stored in a **thread-safe concurrent dictionary** shared across requests within one process. Multiple router replicas each maintain their own copy. For cross-replica consistency, control channel registrations should be broadcast (e.g. via a shared broker topic) so every replica converges on the same table. The routing evaluation itself is stateless and lock-free on read.

---

## Atomicity Dimension

Routing decisions are deterministic for a given routing-table snapshot. If the process crashes after publish but before Ack, the message is redelivered and the same decision is made again (assuming the table has not changed). Registration events should also be durable — publishing registrations through the broker guarantees that table state can be rebuilt from the event log after a crash.

---

## Exercises

1. Participant D registers `conditionKey = "invoices"` with destination `"invoice-processing"`. A message arrives with `MessageType = "invoices"`. Trace the routing path.

2. What happens if Participant D unregisters and a new message with `conditionKey = "invoices"` arrives before any other participant registers for that key?

3. How would you make the routing table consistent across 5 router replicas? Describe a broker-based approach.

---

**Previous: [← Tutorial 10 — Message Filter](10-message-filter.md)** | **Next: [Tutorial 12 — Recipient List →](12-recipient-list.md)**
