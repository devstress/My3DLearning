# Tutorial 19 — Content Filter

## What You'll Learn

- The EIP Content Filter pattern for removing unwanted fields
- How `IContentFilter` / `ContentFilter` strips payloads via JSONPath
- The keep-paths approach: specify what to retain, everything else is removed
- Why smaller payloads improve downstream performance and security
- The complementary relationship with the Content Enricher

---

## EIP Pattern: Content Filter

> *"Use a Content Filter to remove unimportant data items from a message, leaving only the items that are important."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  ┌──────────────────┐    ┌────────────────┐    ┌───────────────┐
  │ Full Payload     │───▶│ Content Filter │───▶│ Slim Payload  │
  │ (20 fields)      │    │ (keepPaths)    │    │ (3 fields)    │
  └──────────────────┘    └────────────────┘    └───────────────┘
```

The Content Filter is the inverse of the Content Enricher. Instead of adding data, it **removes everything except** the specified fields. This produces smaller, focused payloads for downstream consumers that only need a subset of the data.

---

## Platform Implementation

### IContentFilter

```csharp
// src/Processing.Transform/IContentFilter.cs
public interface IContentFilter
{
    Task<string> FilterAsync(
        string payload,
        IReadOnlyList<string> keepPaths,
        CancellationToken cancellationToken = default);
}
```

### ContentFilter (concrete)

The `ContentFilter` class:
1. Parses the incoming JSON payload
2. Evaluates each `keepPaths` entry as a dot-separated property path (e.g. `order.id`, `customer.address.city`)
3. Builds a new JSON document containing **only** the specified paths
4. Paths that don't exist in the payload are silently ignored
5. Returns the filtered JSON string

### Example

```
Input:    { "order": { "id": 1, "total": 99.50, "notes": "..." },
            "customer": { "name": "Alice", "ssn": "123-45-6789" },
            "internal": { "debugTrace": "..." } }

keepPaths: ["order.id", "order.total", "customer.name"]

Output:   { "order": { "id": 1, "total": 99.50 },
            "customer": { "name": "Alice" } }
```

Notice that `customer.ssn` and `internal.debugTrace` are stripped — this is important for **data minimisation** and **security**.

---

## Scalability Dimension

The content filter is **stateless and CPU-light** — JSON parsing and selective copying are fast operations. It scales horizontally without limitation. Filtering *reduces* payload size, which **decreases** downstream broker storage, network bandwidth, and consumer memory usage. In high-throughput pipelines, filtering early in the chain has a multiplier effect on overall system capacity.

---

## Atomicity Dimension

Filtering is a **pure, deterministic function** — the same input and keep-paths always produce the same output. This makes it fully idempotent and safe for retries. The filtered message is published before the source is Acked. If any step fails, the source message is Nacked and redelivered. Since filtering never adds data, there is no risk of inconsistency between retries.

---

## Exercises

1. A message has fields `order.id`, `order.items[]`, `customer.email`, `customer.phone`, `audit.createdBy`. You only need `order.id` and `customer.email`. Write the `keepPaths` list and describe the resulting JSON structure.

2. A keep-path `customer.address.zipCode` is specified but the message doesn't have an `address` field. What happens?

3. Design a pipeline that first enriches a message (Tutorial 18) and then filters it. Why is this order important?

---

**Previous: [← Tutorial 18 — Content Enricher](18-content-enricher.md)** | **Next: [Tutorial 20 — Splitter →](20-splitter.md)**
