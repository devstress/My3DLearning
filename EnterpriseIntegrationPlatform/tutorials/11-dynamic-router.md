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

## Lab

**Objective:** Trace how the Dynamic Router updates its routing table at runtime, analyze the EIP pattern's role in **scalable** integration topologies, and design a consistent routing strategy for distributed deployments.

### Step 1: Trace a Dynamic Registration Flow

Open `src/Processing.Routing/DynamicRouter.cs`. A new participant registers with `conditionKey = "invoices"` and destination `"invoice-processing"`. Then a message arrives with `MessageType = "invoices"`. Trace the code path:

1. How does `RegisterAsync` store the mapping?
2. How does `RouteAsync` look up the destination?
3. What `RoutingDecision` is returned — does it include the matched condition for auditing?

Now: Participant unregisters. A new message with the same key arrives. What happens? Where does the message go?

### Step 2: Design for Multi-Replica Consistency

You have 5 Dynamic Router replicas behind a load balancer. Participant D registers on Replica 1, but Replica 3 doesn't know about it. Design a solution using the platform's broker infrastructure:

- Publish registration events to a `routing.registrations` topic
- Each replica subscribes and updates its local table
- How does this use the **Publish-Subscribe Channel** pattern to keep all replicas consistent?
- What happens to messages during the propagation delay? Is this an **atomicity** concern?

### Step 3: Compare Dynamic Router Scalability vs. Content-Based Router

| Aspect | Content-Based Router | Dynamic Router |
|--------|---------------------|---------------|
| Rule source | Static configuration | Runtime registrations |
| Adding new routes | ? | ? |
| Scalability model | ? | ? |
| Consistency across replicas | ? | ? |

When would you choose a Dynamic Router over a Content-Based Router in a multi-tenant SaaS platform?

## Exam

1. What EIP pattern does the Dynamic Router implement that the Content-Based Router does not?
   - A) Message Filter with discard
   - B) A self-updating routing table where downstream participants register and unregister their interests at runtime, enabling topology changes without redeploying the router
   - C) Priority-based message queuing
   - D) Batch message processing

2. In a horizontally scaled deployment with multiple router instances, what is the main **consistency** challenge?
   - A) All routers must share a single-threaded execution context
   - B) Registration changes on one instance must propagate to all others — during propagation, different instances may route the same message to different destinations
   - C) Dynamic routers cannot be scaled horizontally
   - D) Each router instance requires its own broker connection

3. How does the Dynamic Router pattern support **scalable** integration topology changes?
   - A) It requires a full system restart to add new routes
   - B) New services register their routing interests at startup — the router begins directing matching messages to them immediately, with no configuration changes or redeployments needed
   - C) It pre-allocates routes for all possible message types
   - D) It uses a database trigger to detect new services

---

**Previous: [← Tutorial 10 — Message Filter](10-message-filter.md)** | **Next: [Tutorial 12 — Recipient List →](12-recipient-list.md)**
