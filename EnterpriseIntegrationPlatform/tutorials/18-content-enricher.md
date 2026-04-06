# Tutorial 18 — Content Enricher

## What You'll Learn

- The EIP Content Enricher pattern for augmenting messages with external data
- How `IContentEnricher` / `ContentEnricher` merges external data into the payload
- Enrichment sources: HTTP lookups, database queries, cache
- The merge strategy that preserves existing fields
- How correlation IDs enable tracing through enrichment

---

## EIP Pattern: Content Enricher

> *"Use a Content Enricher to access an external data source in order to augment a message with missing information."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  ┌──────────┐    ┌─────────────────┐    ┌──────────┐
  │ Incoming │───▶│ Content Enricher│───▶│ Enriched │
  │ Message  │    │                 │    │ Message  │
  └──────────┘    └───────┬─────────┘    └──────────┘
                          │
                          ▼
                  ┌───────────────┐
                  │ External Data │
                  │ (HTTP / DB /  │
                  │  Cache)       │
                  └───────────────┘
```

The enricher takes an incomplete message and supplements it with data fetched from an external source. The original payload fields are **never overwritten** — external data is merged in alongside them.

---

## Platform Implementation

### IContentEnricher

```csharp
// src/Processing.Transform/IContentEnricher.cs
public interface IContentEnricher
{
    Task<string> EnrichAsync(
        string payload,
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
```

### ContentEnricher (concrete)

The `ContentEnricher` class:
1. Parses the incoming JSON payload
2. Fetches supplementary data from the configured external source
3. Merges the external data into the JSON document (additive — existing fields preserved)
4. Returns the enriched JSON string

The `correlationId` parameter enables distributed tracing — the enricher passes it to external HTTP calls as a request header, so the entire enrichment chain is traceable.

### Merge Strategy

The enricher performs a **shallow merge** by default:
- New properties from the external source are added to the root object
- Existing properties in the original payload are **not overwritten**
- Nested objects can be merged at configurable depth

```
Original:    { "orderId": 123, "customer": "Alice" }
External:    { "customerTier": "Gold", "region": "EU" }
Enriched:    { "orderId": 123, "customer": "Alice", "customerTier": "Gold", "region": "EU" }
```

---

## Scalability Dimension

The enricher's scalability depends on the **external data source**. The enricher itself is stateless and can be replicated freely, but each replica makes external calls (HTTP, DB). Scaling out enricher replicas increases load on the external service. Mitigate with:
- **Caching**: Cache frequent lookups (e.g. customer tier rarely changes)
- **Bulkheading**: Limit concurrent external calls per replica
- **Circuit breaking**: Fail fast when the external source is down

---

## Atomicity Dimension

Enrichment is **not idempotent by default** if the external data changes between retries. However, because the enricher only *adds* data and never *removes* existing fields, a retry that fetches slightly different external data produces a superset of the original enrichment. The enriched message is published before the source is Acked. If the external call fails, the enricher throws, the source message is Nacked, and the retry policy handles redelivery.

---

## Lab

**Objective:** Design enrichment strategies using external data sources, analyze **atomicity** when enrichment depends on external service availability, and evaluate caching for **scalable** enrichment.

### Step 1: Design a Two-Step Enrichment

An order message `{ "orderId": 42 }` needs customer data, but only contains `orderId` — not `customerId`. Design the enrichment flow:

1. Step 1: Look up `customerId` from `GET /api/orders/42` → returns `{ "customerId": "CUST-7" }`
2. Step 2: Enrich with customer data from `GET /api/customers/CUST-7` → returns `{ "name": "Alice", "tier": "gold" }`

Open `src/Processing.Transform/ContentEnricher.cs` and identify how the enricher merges external data into the envelope. Does it mutate the original or create a new enriched envelope?

### Step 2: Analyze Enrichment Failure Atomicity

The external HTTP service is down during enrichment. Trace what happens:

1. Does the enricher retry? What retry policy applies?
2. If all retries fail, where does the message go?
3. Is the original message preserved untouched for retry later?

Now consider: the enricher calls two services. Service A succeeds but Service B fails. Is the partial enrichment from Service A committed? How does this affect **atomicity**? Design a strategy: should partial enrichment be discarded or preserved?

### Step 3: Design a Caching Strategy for Scalability

At 10,000 messages/second, each enrichment requires an HTTP call to an external CRM. Without caching, that's 10,000 HTTP calls/second. Design a caching strategy:

| Cache Level | TTL | Hit Rate | Scalability Impact |
|-------------|-----|----------|-------------------|
| In-memory (per-worker) | 60s | ~80% | Reduces to 2,000 calls/second |
| Distributed (Redis) | 5min | ~95% | Reduces to 500 calls/second |
| Database fallback | 1hr | ~99% | ? |

Open `src/Processing.Transform/` and check if the platform implements caching. How does cache invalidation interact with message **consistency**?

## Exam

1. The Content Enricher calls an external service that is temporarily unavailable. What is the correct **atomic** behavior?
   - A) Skip enrichment and forward the message without the additional data
   - B) Preserve the original message, retry according to policy, and if all retries fail route to the DLQ — the message is never forwarded with missing enrichment data
   - C) Cache the last known good response and use it
   - D) Block all messages until the external service recovers

2. How does caching in the Content Enricher improve **scalability** without sacrificing data accuracy?
   - A) Caching eliminates the need for external services entirely
   - B) Frequently accessed enrichment data (e.g., customer records) is cached with a TTL — this reduces external API calls by 80-95% while ensuring data freshness through time-based expiration
   - C) The cache stores messages, not enrichment data
   - D) Caching is only useful for batch processing

3. How are the Content Enricher and Content Filter (Tutorial 19) **complementary** in a pipeline?
   - A) They do the same thing in reverse order
   - B) The Enricher adds data from external sources, then the Filter removes fields not needed downstream — together they ensure each consumer receives exactly the data it needs, no more and no less
   - C) The Filter must always run before the Enricher
   - D) They cannot be used in the same pipeline

---

**Previous: [← Tutorial 17 — Normalizer](17-normalizer.md)** | **Next: [Tutorial 19 — Content Filter →](19-content-filter.md)**
