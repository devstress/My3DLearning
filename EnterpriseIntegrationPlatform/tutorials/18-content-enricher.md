# Tutorial 18 — Content Enricher

Augment messages with external data via `IContentEnricher`, merging fetched fields without overwriting existing payload.

---

## Key Types

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

```csharp
// src/Processing.Transform/ContentEnricherOptions.cs
public sealed class ContentEnricherOptions
{
    public string EndpointUrlTemplate { get; init; }
    public string LookupKeyPath { get; init; }
    public string MergeTargetPath { get; init; }
    public bool FallbackOnFailure { get; init; }
    public string? FallbackValue { get; init; }
}
```

```csharp
// src/Processing.Transform/IEnrichmentSource.cs
public interface IEnrichmentSource
{
    Task<JsonNode?> FetchAsync(string key, CancellationToken cancellationToken = default);
}
```

---

## Exercises

### Exercise 1: Merge external data at target path

```csharp
var source = Substitute.For<IEnrichmentSource>();
source.FetchAsync("CUST-1", Arg.Any<CancellationToken>())
    .Returns(JsonNode.Parse("""{"name":"Alice","tier":"Gold"}"""));

var options = Options.Create(new ContentEnricherOptions
{
    EndpointUrlTemplate = "https://api.example.com/customers/{key}",
    LookupKeyPath = "customerId",
    MergeTargetPath = "customer",
});

var enricher = new ContentEnricher(
    source, options, NullLogger<ContentEnricher>.Instance);

var payload = """{"orderId":"ORD-1","customerId":"CUST-1","total":100}""";

var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

using var doc = JsonDocument.Parse(result);
Assert.That(doc.RootElement.GetProperty("orderId").GetString(), Is.EqualTo("ORD-1"));
Assert.That(
    doc.RootElement.GetProperty("customer").GetProperty("name").GetString(),
    Is.EqualTo("Alice"));
Assert.That(
    doc.RootElement.GetProperty("customer").GetProperty("tier").GetString(),
    Is.EqualTo("Gold"));
```

### Exercise 2: Nested lookup key path extracts correct value

```csharp
var source = Substitute.For<IEnrichmentSource>();
source.FetchAsync("ADDR-7", Arg.Any<CancellationToken>())
    .Returns(JsonNode.Parse("""{"city":"Seattle","zip":"98101"}"""));

var options = Options.Create(new ContentEnricherOptions
{
    EndpointUrlTemplate = "https://api.example.com/addresses/{key}",
    LookupKeyPath = "order.addressId",
    MergeTargetPath = "shippingAddress",
});

var enricher = new ContentEnricher(
    source, options, NullLogger<ContentEnricher>.Instance);

var payload = """{"order":{"id":"ORD-2","addressId":"ADDR-7"}}""";

var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

using var doc = JsonDocument.Parse(result);
Assert.That(
    doc.RootElement.GetProperty("shippingAddress").GetProperty("city").GetString(),
    Is.EqualTo("Seattle"));
```

### Exercise 3: Missing lookup key with fallback returns original

```csharp
var source = Substitute.For<IEnrichmentSource>();

var options = Options.Create(new ContentEnricherOptions
{
    EndpointUrlTemplate = "https://api.example.com/{key}",
    LookupKeyPath = "nonExistentField",
    MergeTargetPath = "extra",
    FallbackOnFailure = true,
});

var enricher = new ContentEnricher(
    source, options, NullLogger<ContentEnricher>.Instance);

var payload = """{"id":"X"}""";

var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

using var doc = JsonDocument.Parse(result);
Assert.That(doc.RootElement.GetProperty("id").GetString(), Is.EqualTo("X"));
await source.DidNotReceive().FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
```

### Exercise 4: Source returns null — fallback value merged

```csharp
var source = Substitute.For<IEnrichmentSource>();
source.FetchAsync("KEY-1", Arg.Any<CancellationToken>())
    .Returns((JsonNode?)null);

var options = Options.Create(new ContentEnricherOptions
{
    EndpointUrlTemplate = "https://api.example.com/{key}",
    LookupKeyPath = "key",
    MergeTargetPath = "extra",
    FallbackOnFailure = true,
    FallbackValue = """{"status":"unknown"}""",
});

var enricher = new ContentEnricher(
    source, options, NullLogger<ContentEnricher>.Instance);

var payload = """{"key":"KEY-1"}""";

var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

using var doc = JsonDocument.Parse(result);
Assert.That(
    doc.RootElement.GetProperty("extra").GetProperty("status").GetString(),
    Is.EqualTo("unknown"));
```

### Exercise 5: Enrichment preserves all existing fields

```csharp
var source = Substitute.For<IEnrichmentSource>();
source.FetchAsync("C-1", Arg.Any<CancellationToken>())
    .Returns(JsonNode.Parse("""{"loyalty":true}"""));

var options = Options.Create(new ContentEnricherOptions
{
    EndpointUrlTemplate = "https://api.example.com/{key}",
    LookupKeyPath = "cid",
    MergeTargetPath = "loyalty",
});

var enricher = new ContentEnricher(
    source, options, NullLogger<ContentEnricher>.Instance);

var payload = """{"cid":"C-1","amount":50,"currency":"USD"}""";

var result = await enricher.EnrichAsync(payload, Guid.NewGuid());

using var doc = JsonDocument.Parse(result);
Assert.That(doc.RootElement.GetProperty("cid").GetString(), Is.EqualTo("C-1"));
Assert.That(doc.RootElement.GetProperty("amount").GetInt32(), Is.EqualTo(50));
Assert.That(doc.RootElement.GetProperty("currency").GetString(), Is.EqualTo("USD"));
```

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial18/Lab.cs`](../tests/TutorialLabs/Tutorial18/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial18.Lab"
```

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial18/Exam.cs`](../tests/TutorialLabs/Tutorial18/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial18.Exam"
```

---

**Previous: [← Tutorial 17 — Normalizer](17-normalizer.md)** | **Next: [Tutorial 19 — Content Filter →](19-content-filter.md)**
