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

## Exercises

1. An order message `{ "orderId": 42 }` needs enrichment with customer data from `GET /api/customers/{customerId}`. But the order message doesn't contain `customerId` — only `orderId`. How would you design a two-step enrichment?

2. The external HTTP service is down. What happens to messages waiting for enrichment? How does the retry policy interact with the enricher?

3. Compare the Content Enricher to the Content Filter (Tutorial 19). How are they complementary?

---

**Previous: [← Tutorial 17 — Normalizer](17-normalizer.md)** | **Next: [Tutorial 19 — Content Filter →](19-content-filter.md)**
