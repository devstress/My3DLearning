# Tutorial 09 — Content-Based Router

Priority-ordered routing rules with `IContentBasedRouter`, `RoutingRule`, `RoutingOperator`, and `RoutingDecision`.

---

## Key Types

```csharp
// src/Processing.Routing/IContentBasedRouter.cs
public interface IContentBasedRouter
{
    Task<RoutingDecision> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Routing/RoutingRule.cs
public sealed record RoutingRule
{
    public required int Priority { get; init; }
    public required string FieldName { get; init; }
    public required RoutingOperator Operator { get; init; }
    public required string Value { get; init; }
    public required string TargetTopic { get; init; }
    public string? Name { get; init; }
}
```

```csharp
// src/Processing.Routing/RoutingOperator.cs
public enum RoutingOperator
{
    Equals,
    Contains,
    StartsWith,
    Regex
}
```

```csharp
// src/Processing.Routing/RoutingDecision.cs
public sealed record RoutingDecision(
    string TargetTopic,
    RoutingRule? MatchedRule,
    bool IsDefault);
```

---

## Exercises

### 1. Route by MessageType with Equals operator

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new RouterOptions
{
    Rules =
    [
        new RoutingRule
        {
            Priority = 1, FieldName = "MessageType",
            Operator = RoutingOperator.Equals,
            Value = "order.created", TargetTopic = "orders-topic",
            Name = "OrderCreated",
        },
        new RoutingRule
        {
            Priority = 2, FieldName = "MessageType",
            Operator = RoutingOperator.Equals,
            Value = "payment.received", TargetTopic = "payments-topic",
            Name = "PaymentReceived",
        },
    ],
    DefaultTopic = "unmatched-topic",
});

var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "order-data", "OrderService", "order.created");

var decision = await router.RouteAsync(envelope);

Assert.That(decision.TargetTopic, Is.EqualTo("orders-topic"));
Assert.That(decision.IsDefault, Is.False);
Assert.That(decision.MatchedRule, Is.Not.Null);
Assert.That(decision.MatchedRule!.Name, Is.EqualTo("OrderCreated"));
```

### 2. Route by Metadata field with Contains operator

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new RouterOptions
{
    Rules =
    [
        new RoutingRule
        {
            Priority = 1, FieldName = "Metadata.region",
            Operator = RoutingOperator.Contains,
            Value = "europe", TargetTopic = "eu-topic",
            Name = "EuropeRegion",
        },
    ],
    DefaultTopic = "global-topic",
});

var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "eu-data", "RegionalService", "data.regional") with
{
    Metadata = new Dictionary<string, string> { ["region"] = "western-europe-1" },
};

var decision = await router.RouteAsync(envelope);

Assert.That(decision.TargetTopic, Is.EqualTo("eu-topic"));
Assert.That(decision.IsDefault, Is.False);
Assert.That(decision.MatchedRule!.Name, Is.EqualTo("EuropeRegion"));
```

### 3. Route by MessageType with Regex operator

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new RouterOptions
{
    Rules =
    [
        new RoutingRule
        {
            Priority = 1, FieldName = "MessageType",
            Operator = RoutingOperator.Regex,
            Value = @"^order\..+", TargetTopic = "order-events",
            Name = "AllOrderEvents",
        },
    ],
    DefaultTopic = "other-events",
});

var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "shipped-data", "OrderService", "order.shipped");

var decision = await router.RouteAsync(envelope);

Assert.That(decision.TargetTopic, Is.EqualTo("order-events"));
Assert.That(decision.MatchedRule!.Name, Is.EqualTo("AllOrderEvents"));
```

### 4. No rule matches — falls back to default topic

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new RouterOptions
{
    Rules =
    [
        new RoutingRule
        {
            Priority = 1, FieldName = "MessageType",
            Operator = RoutingOperator.Equals,
            Value = "order.created", TargetTopic = "orders-topic",
        },
    ],
    DefaultTopic = "catch-all-topic",
});

var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "unknown-data", "UnknownService", "unknown.event");

var decision = await router.RouteAsync(envelope);

Assert.That(decision.TargetTopic, Is.EqualTo("catch-all-topic"));
Assert.That(decision.IsDefault, Is.True);
Assert.That(decision.MatchedRule, Is.Null);
```

### 5. RoutingDecision exposes full matched rule details

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new RouterOptions
{
    Rules =
    [
        new RoutingRule
        {
            Priority = 10, FieldName = "Source",
            Operator = RoutingOperator.Equals,
            Value = "CriticalService", TargetTopic = "critical-topic",
            Name = "CriticalSource",
        },
    ],
    DefaultTopic = "default-topic",
});

var router = new ContentBasedRouter(producer, options, NullLogger<ContentBasedRouter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "critical-payload", "CriticalService", "alert.triggered");

var decision = await router.RouteAsync(envelope);

Assert.That(decision.MatchedRule, Is.Not.Null);
Assert.That(decision.MatchedRule!.Priority, Is.EqualTo(10));
Assert.That(decision.MatchedRule.FieldName, Is.EqualTo("Source"));
Assert.That(decision.MatchedRule.Operator, Is.EqualTo(RoutingOperator.Equals));
Assert.That(decision.MatchedRule.Value, Is.EqualTo("CriticalService"));
Assert.That(decision.MatchedRule.TargetTopic, Is.EqualTo("critical-topic"));
Assert.That(decision.MatchedRule.Name, Is.EqualTo("CriticalSource"));
```

---

## Lab

> 💻 [`tests/TutorialLabs/Tutorial09/Lab.cs`](../tests/TutorialLabs/Tutorial09/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial09.Lab"
```

## Exam

> 💻 [`tests/TutorialLabs/Tutorial09/Exam.cs`](../tests/TutorialLabs/Tutorial09/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial09.Exam"
```

---

**Previous: [← Tutorial 08 — Activities and the Pipeline](08-activities-pipeline.md)** | **Next: [Tutorial 10 — Message Filter →](10-message-filter.md)**
