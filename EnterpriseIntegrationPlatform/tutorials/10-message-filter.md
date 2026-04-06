# Tutorial 10 — Message Filter

Predicate-based filtering with `IMessageFilter`, `MessageFilterResult`, `MessageFilterOptions`, and `RuleCondition`.

---

## Key Types

```csharp
// src/Processing.Routing/IMessageFilter.cs
public interface IMessageFilter
{
    Task<MessageFilterResult> FilterAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Routing/MessageFilterResult.cs
public sealed record MessageFilterResult(
    bool Passed,
    string? OutputTopic,
    string Reason);
```

```csharp
// src/Processing.Routing/MessageFilterOptions.cs
public sealed class MessageFilterOptions
{
    public IReadOnlyList<RuleCondition> Conditions { get; init; } = [];
    public RuleLogicOperator Logic { get; init; } = RuleLogicOperator.And;
    public required string OutputTopic { get; init; }
    public string? DiscardTopic { get; init; }
    public bool RequireDiscardTopic { get; init; }
}
```

```csharp
// src/RuleEngine/RuleCondition.cs
public sealed record RuleCondition
{
    public required string FieldName { get; init; }
    public required RuleConditionOperator Operator { get; init; }
    public required string Value { get; init; }
}
```

---

## Exercises

### 1. Accept filter: message passes when predicate matches

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new MessageFilterOptions
{
    Conditions =
    [
        new RuleCondition
        {
            FieldName = "MessageType",
            Operator = RuleConditionOperator.Equals,
            Value = "order.created",
        },
    ],
    Logic = RuleLogicOperator.And,
    OutputTopic = "orders-accepted",
    DiscardTopic = "orders-rejected",
});

var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "valid-order", "OrderService", "order.created");

var result = await filter.FilterAsync(envelope);

Assert.That(result.Passed, Is.True);
Assert.That(result.OutputTopic, Is.EqualTo("orders-accepted"));
Assert.That(result.Reason, Is.EqualTo("Predicate matched"));

await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Is("orders-accepted"),
    Arg.Any<CancellationToken>());
```

### 2. Reject filter: message discarded to DLQ

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new MessageFilterOptions
{
    Conditions =
    [
        new RuleCondition
        {
            FieldName = "MessageType",
            Operator = RuleConditionOperator.Equals,
            Value = "order.created",
        },
    ],
    Logic = RuleLogicOperator.And,
    OutputTopic = "orders-accepted",
    DiscardTopic = "orders-rejected",
});

var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "unknown-data", "UnknownService", "unknown.event");

var result = await filter.FilterAsync(envelope);

Assert.That(result.Passed, Is.False);
Assert.That(result.OutputTopic, Is.EqualTo("orders-rejected"));
Assert.That(result.Reason, Does.Contain("discard"));

await producer.Received(1).PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Is("orders-rejected"),
    Arg.Any<CancellationToken>());
```

### 3. No conditions configured — everything passes through

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new MessageFilterOptions
{
    Conditions = [],
    OutputTopic = "pass-through-topic",
});

var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "any-data", "AnyService", "any.event");

var result = await filter.FilterAsync(envelope);

Assert.That(result.Passed, Is.True);
Assert.That(result.OutputTopic, Is.EqualTo("pass-through-topic"));
```

### 4. No discard topic — silent drop, no publish

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new MessageFilterOptions
{
    Conditions =
    [
        new RuleCondition
        {
            FieldName = "MessageType",
            Operator = RuleConditionOperator.Equals,
            Value = "expected.type",
        },
    ],
    Logic = RuleLogicOperator.And,
    OutputTopic = "output-topic",
});

var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

var envelope = IntegrationEnvelope<string>.Create(
    "wrong-data", "Service", "wrong.type");

var result = await filter.FilterAsync(envelope);

Assert.That(result.Passed, Is.False);
Assert.That(result.OutputTopic, Is.Null);
Assert.That(result.Reason, Does.Contain("silently discarded"));

await producer.DidNotReceive().PublishAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Any<string>(),
    Arg.Any<CancellationToken>());
```

### 5. Filter result contains correct reason and topic for both outcomes

```csharp
var producer = Substitute.For<IMessageBrokerProducer>();

var options = Options.Create(new MessageFilterOptions
{
    Conditions =
    [
        new RuleCondition
        {
            FieldName = "Source",
            Operator = RuleConditionOperator.Equals,
            Value = "TrustedService",
        },
    ],
    Logic = RuleLogicOperator.And,
    OutputTopic = "trusted-output",
    DiscardTopic = "untrusted-dlq",
});

var filter = new MessageFilter(producer, options, NullLogger<MessageFilter>.Instance);

var trusted = IntegrationEnvelope<string>.Create(
    "trusted-data", "TrustedService", "data.event");
var passResult = await filter.FilterAsync(trusted);
Assert.That(passResult.Passed, Is.True);
Assert.That(passResult.Reason, Is.EqualTo("Predicate matched"));

var untrusted = IntegrationEnvelope<string>.Create(
    "untrusted-data", "UntrustedService", "data.event");
var failResult = await filter.FilterAsync(untrusted);
Assert.That(failResult.Passed, Is.False);
Assert.That(failResult.OutputTopic, Is.EqualTo("untrusted-dlq"));
Assert.That(failResult.Reason, Does.Contain("discard"));
```

---

## Lab

> 💻 [`tests/TutorialLabs/Tutorial10/Lab.cs`](../tests/TutorialLabs/Tutorial10/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial10.Lab"
```

## Exam

> 💻 [`tests/TutorialLabs/Tutorial10/Exam.cs`](../tests/TutorialLabs/Tutorial10/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial10.Exam"
```

---

**Previous: [← Tutorial 09 — Content-Based Router](09-content-based-router.md)** | **Next: [Tutorial 11 — Dynamic Router →](11-dynamic-router.md)**
