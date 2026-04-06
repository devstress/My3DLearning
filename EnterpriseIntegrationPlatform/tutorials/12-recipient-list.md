# Tutorial 12 — Recipient List

Fan-out routing that sends the same message to every resolved destination, with rule-based and metadata-based recipient resolution and deduplication.

---

## Key Types

```csharp
// src/Processing.Routing/IRecipientList.cs
public interface IRecipientList
{
    Task<RecipientListResult> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}

// src/Processing.Routing/RecipientListResult.cs
public sealed record RecipientListResult(
    IReadOnlyList<string> Destinations,
    int ResolvedCount,
    int DuplicatesRemoved);

// src/Processing.Routing/RecipientListRule.cs (used in RecipientListOptions)
public sealed class RecipientListRule
{
    public string FieldName { get; set; }
    public RoutingOperator Operator { get; set; }
    public string Value { get; set; }
    public List<string> Destinations { get; set; }
    public string? Name { get; set; }
}
```

---

## Exercises

### 1. Single rule matches — fan-out to multiple destinations

```csharp
var options = Options.Create(new RecipientListOptions
{
    Rules =
    [
        new RecipientListRule
        {
            FieldName = "MessageType",
            Operator = RoutingOperator.Equals,
            Value = "order.created",
            Destinations = ["audit-topic", "analytics-topic", "fulfilment-topic"],
            Name = "OrderFanOut",
        },
    ],
});

var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "order-data", "OrderService", "order.created");

var result = await router.RouteAsync(envelope);

Assert.That(result.ResolvedCount, Is.EqualTo(3));
Assert.That(result.Destinations, Contains.Item("audit-topic"));
Assert.That(result.Destinations, Contains.Item("analytics-topic"));
Assert.That(result.Destinations, Contains.Item("fulfilment-topic"));
```

### 2. Duplicate destinations are deduplicated

```csharp
var options = Options.Create(new RecipientListOptions
{
    Rules =
    [
        new RecipientListRule
        {
            FieldName = "MessageType",
            Operator = RoutingOperator.Contains,
            Value = "order",
            Destinations = ["audit-topic", "analytics-topic"],
        },
        new RecipientListRule
        {
            FieldName = "Source",
            Operator = RoutingOperator.Equals,
            Value = "OrderService",
            Destinations = ["audit-topic", "fulfilment-topic"],
        },
    ],
});

var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "order-data", "OrderService", "order.created");

var result = await router.RouteAsync(envelope);

Assert.That(result.ResolvedCount, Is.EqualTo(3));
Assert.That(result.DuplicatesRemoved, Is.EqualTo(1));
Assert.That(result.Destinations, Contains.Item("audit-topic"));
Assert.That(result.Destinations, Contains.Item("analytics-topic"));
Assert.That(result.Destinations, Contains.Item("fulfilment-topic"));
```

### 3. No rule matches — empty result

```csharp
var options = Options.Create(new RecipientListOptions
{
    Rules =
    [
        new RecipientListRule
        {
            FieldName = "MessageType",
            Operator = RoutingOperator.Equals,
            Value = "order.created",
            Destinations = ["orders-topic"],
        },
    ],
});

var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "payment-data", "PaymentService", "payment.received");

var result = await router.RouteAsync(envelope);

Assert.That(result.ResolvedCount, Is.EqualTo(0));
Assert.That(result.Destinations, Is.Empty);
```

### 4. Metadata-based recipient resolution

```csharp
var options = Options.Create(new RecipientListOptions
{
    Rules = [],
    MetadataRecipientsKey = "recipients",
});

var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "data", "Service", "event.occurred") with
{
    Metadata = new Dictionary<string, string>
    {
        ["recipients"] = "topic-a,topic-b,topic-c",
    },
};

var result = await router.RouteAsync(envelope);

Assert.That(result.ResolvedCount, Is.EqualTo(3));
Assert.That(result.Destinations, Contains.Item("topic-a"));
Assert.That(result.Destinations, Contains.Item("topic-b"));
Assert.That(result.Destinations, Contains.Item("topic-c"));
```

### 5. Verify producer receives all publish calls

```csharp
var options = Options.Create(new RecipientListOptions
{
    Rules =
    [
        new RecipientListRule
        {
            FieldName = "MessageType",
            Operator = RoutingOperator.Equals,
            Value = "order.created",
            Destinations = ["topic-a", "topic-b"],
        },
    ],
});

var router = new RecipientListRouter(producer, options, NullLogger<RecipientListRouter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "data", "OrderService", "order.created");

await router.RouteAsync(envelope);

await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Is("topic-a"),
    Arg.Any<CancellationToken>());

await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Is("topic-b"),
    Arg.Any<CancellationToken>());
```

---

## Lab

> 💻 [`tests/TutorialLabs/Tutorial12/Lab.cs`](../tests/TutorialLabs/Tutorial12/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial12.Lab"
```

## Exam

> 💻 [`tests/TutorialLabs/Tutorial12/Exam.cs`](../tests/TutorialLabs/Tutorial12/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial12.Exam"
```

---

**Previous: [← Tutorial 11 — Dynamic Router](11-dynamic-router.md)** | **Next: [Tutorial 13 — Routing Slip →](13-routing-slip.md)**
