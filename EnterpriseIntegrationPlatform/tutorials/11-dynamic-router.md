# Tutorial 11 — Dynamic Router

A router whose routing table is built at runtime as downstream participants register and unregister through a control channel.

---

## Key Types

```csharp
// src/Processing.Routing/IDynamicRouter.cs
public interface IDynamicRouter
{
    Task<DynamicRoutingDecision> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}

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

// src/Processing.Routing/DynamicRoutingDecision.cs
public sealed record DynamicRoutingDecision(
    string Destination,
    DynamicRouteEntry? MatchedEntry,
    bool IsFallback,
    string? ConditionValue);

// src/Processing.Routing/DynamicRouteEntry.cs
public sealed record DynamicRouteEntry(
    string ConditionKey,
    string Destination,
    string? ParticipantId,
    DateTimeOffset RegisteredAtUtc);
```

---

## Exercises

### 1. Register a route and route a matching message

```csharp
var options = Options.Create(new DynamicRouterOptions
{
    ConditionField = "MessageType",
    FallbackTopic = "unmatched-topic",
});

var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

await router.RegisterAsync("order.created", "orders-topic", "OrderService");

var envelope = IntegrationEnvelope<string>.Create(
    "order-data", "OrderService", "order.created");

var decision = await router.RouteAsync(envelope);

Assert.That(decision.Destination, Is.EqualTo("orders-topic"));
Assert.That(decision.IsFallback, Is.False);
Assert.That(decision.MatchedEntry, Is.Not.Null);
Assert.That(decision.MatchedEntry!.ParticipantId, Is.EqualTo("OrderService"));
Assert.That(decision.ConditionValue, Is.EqualTo("order.created"));
```

### 2. Unmatched message falls back to FallbackTopic

```csharp
var options = Options.Create(new DynamicRouterOptions
{
    ConditionField = "MessageType",
    FallbackTopic = "catch-all-topic",
});

var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "unknown-data", "UnknownService", "unknown.event");

var decision = await router.RouteAsync(envelope);

Assert.That(decision.Destination, Is.EqualTo("catch-all-topic"));
Assert.That(decision.IsFallback, Is.True);
Assert.That(decision.MatchedEntry, Is.Null);
```

### 3. Unregister removes route — subsequent message uses fallback

```csharp
var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

await router.RegisterAsync("order.created", "orders-topic");

var removed = await router.UnregisterAsync("order.created");
Assert.That(removed, Is.True);

var envelope = IntegrationEnvelope<string>.Create(
    "order-data", "OrderService", "order.created");

var decision = await router.RouteAsync(envelope);
Assert.That(decision.IsFallback, Is.True);
Assert.That(decision.Destination, Is.EqualTo("fallback-topic"));
```

### 4. Case-insensitive routing matches regardless of case

```csharp
var options = Options.Create(new DynamicRouterOptions
{
    ConditionField = "MessageType",
    FallbackTopic = "fallback",
    CaseInsensitive = true,
});

var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

await router.RegisterAsync("order.created", "orders-topic");

var envelope = IntegrationEnvelope<string>.Create(
    "data", "Service", "Order.Created");

var decision = await router.RouteAsync(envelope);

Assert.That(decision.Destination, Is.EqualTo("orders-topic"));
Assert.That(decision.IsFallback, Is.False);
```

### 5. Route by metadata field

```csharp
var options = Options.Create(new DynamicRouterOptions
{
    ConditionField = "Metadata.region",
    FallbackTopic = "global-topic",
});

var router = new DynamicRouter(producer, options, NullLogger<DynamicRouter>.Instance);

await router.RegisterAsync("eu-west", "eu-west-topic", "EUService");

var envelope = IntegrationEnvelope<string>.Create(
    "eu-data", "RegionalService", "data.sync") with
{
    Metadata = new Dictionary<string, string>
    {
        ["region"] = "eu-west",
    },
};

var decision = await router.RouteAsync(envelope);

Assert.That(decision.Destination, Is.EqualTo("eu-west-topic"));
Assert.That(decision.IsFallback, Is.False);
Assert.That(decision.MatchedEntry!.ParticipantId, Is.EqualTo("EUService"));
```

---

## Lab

> 💻 [`tests/TutorialLabs/Tutorial11/Lab.cs`](../tests/TutorialLabs/Tutorial11/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial11.Lab"
```

## Exam

> 💻 [`tests/TutorialLabs/Tutorial11/Exam.cs`](../tests/TutorialLabs/Tutorial11/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial11.Exam"
```

---

**Previous: [← Tutorial 10 — Message Filter](10-message-filter.md)** | **Next: [Tutorial 12 — Recipient List →](12-recipient-list.md)**
