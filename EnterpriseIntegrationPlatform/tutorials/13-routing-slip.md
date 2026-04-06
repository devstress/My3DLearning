# Tutorial 13 — Routing Slip

Each message carries its own processing itinerary — steps are executed sequentially and the slip advances after each one.

---

## Key Types

```csharp
// src/Contracts/RoutingSlip.cs
public sealed record RoutingSlip(IReadOnlyList<RoutingSlipStep> Steps)
{
    public const string MetadataKey = "RoutingSlip";
    public bool IsComplete => Steps.Count == 0;
    public RoutingSlipStep? CurrentStep => Steps.Count > 0 ? Steps[0] : null;

    public RoutingSlip Advance()
    {
        if (IsComplete)
            throw new InvalidOperationException("Cannot advance a completed routing slip.");
        return new RoutingSlip(Steps.Skip(1).ToList().AsReadOnly());
    }
}

// src/Contracts/RoutingSlipStep.cs
public sealed record RoutingSlipStep(
    string StepName,
    string? DestinationTopic = null,
    IReadOnlyDictionary<string, string>? Parameters = null);

// src/Processing.Routing/IRoutingSlipRouter.cs
public interface IRoutingSlipRouter
{
    Task<RoutingSlipStepResult> ExecuteCurrentStepAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}

// src/Processing.Routing/IRoutingSlipStepHandler.cs
public interface IRoutingSlipStepHandler
{
    string StepName { get; }
    Task<bool> HandleAsync<T>(
        IntegrationEnvelope<T> envelope,
        IReadOnlyDictionary<string, string>? parameters,
        CancellationToken cancellationToken = default);
}

// src/Processing.Routing/RoutingSlipStepResult.cs
public sealed record RoutingSlipStepResult(
    string StepName,
    bool Succeeded,
    string? FailureReason,
    RoutingSlip RemainingSlip,
    string? ForwardedToTopic);
```

---

## Exercises

### 1. Execute a single step successfully

```csharp
var handler = Substitute.For<IRoutingSlipStepHandler>();
handler.StepName.Returns("Validate");
handler.HandleAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Any<IReadOnlyDictionary<string, string>?>(),
    Arg.Any<CancellationToken>())
    .Returns(true);

var router = new RoutingSlipRouter(
    [handler], producer, NullLogger<RoutingSlipRouter>.Instance);

var slip = new RoutingSlip([new RoutingSlipStep("Validate", "output-topic")]);
var envelope = IntegrationEnvelope<string>.Create(
    "payload", "Service", "event.type") with
{
    Metadata = new Dictionary<string, string>
    {
        [RoutingSlip.MetadataKey] = JsonSerializer.Serialize(slip.Steps),
    },
};

var result = await router.ExecuteCurrentStepAsync(envelope);

Assert.That(result.StepName, Is.EqualTo("Validate"));
Assert.That(result.Succeeded, Is.True);
Assert.That(result.FailureReason, Is.Null);
Assert.That(result.RemainingSlip.IsComplete, Is.True);
Assert.That(result.ForwardedToTopic, Is.EqualTo("output-topic"));
```

### 2. Multi-step slip — advance through steps

```csharp
var slip = new RoutingSlip([
    new RoutingSlipStep("Validate"),
    new RoutingSlipStep("Transform", "transform-topic"),
]);

var envelope = IntegrationEnvelope<string>.Create(
    "payload", "Service", "event.type") with
{
    Metadata = new Dictionary<string, string>
    {
        [RoutingSlip.MetadataKey] = JsonSerializer.Serialize(slip.Steps),
    },
};

var result = await router.ExecuteCurrentStepAsync(envelope);

Assert.That(result.StepName, Is.EqualTo("Validate"));
Assert.That(result.Succeeded, Is.True);
Assert.That(result.RemainingSlip.Steps, Has.Count.EqualTo(1));
Assert.That(result.RemainingSlip.CurrentStep!.StepName, Is.EqualTo("Transform"));
Assert.That(result.ForwardedToTopic, Is.Null);
```

### 3. Step with parameters passed to handler

```csharp
IReadOnlyDictionary<string, string>? receivedParams = null;

var handler = Substitute.For<IRoutingSlipStepHandler>();
handler.StepName.Returns("Enrich");
handler.HandleAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Any<IReadOnlyDictionary<string, string>?>(),
    Arg.Any<CancellationToken>())
    .Returns(ci =>
    {
        receivedParams = ci.ArgAt<IReadOnlyDictionary<string, string>?>(1);
        return true;
    });

var parameters = new Dictionary<string, string>
{
    ["lookupUrl"] = "https://api.example.com/enrich",
    ["timeout"] = "30",
};

var slip = new RoutingSlip([new RoutingSlipStep("Enrich", null, parameters)]);
var envelope = IntegrationEnvelope<string>.Create(
    "payload", "Service", "event.type") with
{
    Metadata = new Dictionary<string, string>
    {
        [RoutingSlip.MetadataKey] = JsonSerializer.Serialize(slip.Steps),
    },
};

var result = await router.ExecuteCurrentStepAsync(envelope);

Assert.That(result.Succeeded, Is.True);
Assert.That(receivedParams, Is.Not.Null);
Assert.That(receivedParams!["lookupUrl"], Is.EqualTo("https://api.example.com/enrich"));
Assert.That(receivedParams["timeout"], Is.EqualTo("30"));
```

### 4. RoutingSlip.Advance() consumes current step

```csharp
var slip = new RoutingSlip([
    new RoutingSlipStep("Step1"),
    new RoutingSlipStep("Step2"),
    new RoutingSlipStep("Step3"),
]);

Assert.That(slip.IsComplete, Is.False);
Assert.That(slip.CurrentStep!.StepName, Is.EqualTo("Step1"));

var advanced = slip.Advance();
Assert.That(advanced.CurrentStep!.StepName, Is.EqualTo("Step2"));
Assert.That(advanced.Steps, Has.Count.EqualTo(2));

var advanced2 = advanced.Advance();
Assert.That(advanced2.CurrentStep!.StepName, Is.EqualTo("Step3"));

var completed = advanced2.Advance();
Assert.That(completed.IsComplete, Is.True);
Assert.That(completed.CurrentStep, Is.Null);
```

### 5. Handler throws exception — step fails gracefully

```csharp
var handler = Substitute.For<IRoutingSlipStepHandler>();
handler.StepName.Returns("RiskyStep");
handler.HandleAsync(
    Arg.Any<IntegrationEnvelope<string>>(),
    Arg.Any<IReadOnlyDictionary<string, string>?>(),
    Arg.Any<CancellationToken>())
    .Returns<bool>(_ => throw new InvalidOperationException("Connection timed out"));

var router = new RoutingSlipRouter(
    [handler], producer, NullLogger<RoutingSlipRouter>.Instance);

var slip = new RoutingSlip([new RoutingSlipStep("RiskyStep", "output-topic")]);
var envelope = IntegrationEnvelope<string>.Create(
    "payload", "Service", "event.type") with
{
    Metadata = new Dictionary<string, string>
    {
        [RoutingSlip.MetadataKey] = JsonSerializer.Serialize(slip.Steps),
    },
};

var result = await router.ExecuteCurrentStepAsync(envelope);

Assert.That(result.Succeeded, Is.False);
Assert.That(result.FailureReason, Does.Contain("Connection timed out"));
Assert.That(result.ForwardedToTopic, Is.Null);
```

---

## Lab

> 💻 [`tests/TutorialLabs/Tutorial13/Lab.cs`](../tests/TutorialLabs/Tutorial13/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial13.Lab"
```

## Exam

> 💻 [`tests/TutorialLabs/Tutorial13/Exam.cs`](../tests/TutorialLabs/Tutorial13/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial13.Exam"
```

---

**Previous: [← Tutorial 12 — Recipient List](12-recipient-list.md)** | **Next: [Tutorial 14 — Process Manager →](14-process-manager.md)**
