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

## Lab

**Objective:** Apply the Content Filter pattern to remove unnecessary data, analyze data minimization for **security** and **scalability**, and design a filter-then-route pipeline.

### Step 1: Configure a Content Filter

A message has fields: `order.id`, `order.items[]`, `customer.email`, `customer.phone`, `customer.ssn`, `audit.createdBy`. You need only `order.id` and `customer.email` for the downstream billing system. Write the `keepPaths` configuration:

```csharp
var keepPaths = new[] { "order.id", "customer.email" };
```

Open `src/Processing.Transformer/JsonPathFilterStep.cs` and trace: What happens to `customer.ssn`? What happens if `keepPaths` references a field that doesn't exist in the message (e.g., `customer.address.zipCode`)?

### Step 2: Design for Security and Data Minimization

The Content Filter is a key tool for **data minimization** (GDPR, PCI-DSS). Design a pipeline:

| Consumer | Allowed Fields | Filtered Fields |
|----------|---------------|----------------|
| Billing | `order.id`, `customer.email`, `order.total` | PII, items, audit |
| Analytics | `order.id`, `order.items[]`, `order.total` | All customer PII |
| Audit | All fields | None (full record) |

How does the Content Filter ensure that the billing system **never** receives `customer.ssn`? Why is this an **atomicity** concern — what happens if the filter is accidentally misconfigured?

### Step 3: Design an Enrich-Then-Filter Pipeline

Explain why the order matters: first Enrich (Tutorial 18) then Filter. Draw a pipeline:

```
Raw message → Content Enricher (add customer data) → Content Filter (remove sensitive fields) → Route to consumer
```

If you reverse the order (filter first, then enrich), what goes wrong? How does the pipeline order preserve both data completeness and data minimization?

## Exam

1. A keep-path references a field that doesn't exist in the message. What should the Content Filter do?
   - A) Throw an exception and route to DLQ
   - B) Silently omit the missing field from the output — the filter operates on what's present, producing a valid subset without failing, which supports graceful handling of schema variations
   - C) Add the field with a null value
   - D) Block the message until the field is available

2. Why is the Content Filter critical for **PCI-DSS and GDPR compliance** in enterprise integration?
   - A) It encrypts sensitive fields automatically
   - B) It ensures each downstream consumer receives only the data it needs — preventing over-exposure of PII and cardholder data by stripping unauthorized fields before routing
   - C) It logs all sensitive data access for audit
   - D) It replaces sensitive data with synthetic values

3. In a high-throughput pipeline, how does content filtering improve **scalability**?
   - A) Filtering doesn't affect performance
   - B) Removing unnecessary fields reduces message size — smaller messages mean lower broker storage costs, faster serialization, and reduced network bandwidth across the entire downstream processing chain
   - C) Filtering enables parallel processing
   - D) Filtered messages skip the routing step

---

**Previous: [← Tutorial 18 — Content Enricher](18-content-enricher.md)** | **Next: [Tutorial 20 — Splitter →](20-splitter.md)**
