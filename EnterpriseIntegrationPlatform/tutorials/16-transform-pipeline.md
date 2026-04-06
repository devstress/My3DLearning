# Tutorial 16 — Transform Pipeline

Chain ordered `ITransformStep` instances through an `ITransformPipeline` to transform payloads in sequence.

---

## Key Types

```csharp
// src/Processing.Transform/ITransformPipeline.cs
public interface ITransformPipeline
{
    Task<TransformResult> ExecuteAsync(
        string payload,
        string contentType,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Transform/ITransformStep.cs
public interface ITransformStep
{
    string Name { get; }
    Task<TransformContext> ExecuteAsync(
        TransformContext context,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/Processing.Transform/TransformContext.cs
public sealed class TransformContext
{
    public string Payload { get; }
    public string ContentType { get; }
    public Dictionary<string, string> Metadata { get; init; } = new();

    public TransformContext WithPayload(string payload, string contentType) => ...;
    public TransformContext WithPayload(string payload) => ...;
}
```

```csharp
// src/Processing.Transform/TransformResult.cs
public sealed record TransformResult(
    string Payload,
    string ContentType,
    int StepsApplied,
    IReadOnlyDictionary<string, string> Metadata);
```

---

## Exercises

### Exercise 1: Single step transforms payload

```csharp
var step = Substitute.For<ITransformStep>();
step.Name.Returns("Upper");
step.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
    .Returns(ci =>
    {
        var ctx = ci.Arg<TransformContext>();
        return ctx.WithPayload(ctx.Payload.ToUpperInvariant());
    });

var options = Options.Create(new TransformOptions());
var pipeline = new TransformPipeline(
    new[] { step }, options, NullLogger<TransformPipeline>.Instance);

var result = await pipeline.ExecuteAsync("hello", "text/plain");

Assert.That(result.Payload, Is.EqualTo("HELLO"));
Assert.That(result.StepsApplied, Is.EqualTo(1));
Assert.That(result.ContentType, Is.EqualTo("text/plain"));
```

### Exercise 2: Multiple steps applied in order

```csharp
var step1 = Substitute.For<ITransformStep>();
step1.Name.Returns("Append-A");
step1.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
    .Returns(ci => ci.Arg<TransformContext>().WithPayload(ci.Arg<TransformContext>().Payload + "A"));

var step2 = Substitute.For<ITransformStep>();
step2.Name.Returns("Append-B");
step2.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
    .Returns(ci => ci.Arg<TransformContext>().WithPayload(ci.Arg<TransformContext>().Payload + "B"));

var options = Options.Create(new TransformOptions());
var pipeline = new TransformPipeline(
    new[] { step1, step2 }, options, NullLogger<TransformPipeline>.Instance);

var result = await pipeline.ExecuteAsync("X", "text/plain");

Assert.That(result.Payload, Is.EqualTo("XAB"));
Assert.That(result.StepsApplied, Is.EqualTo(2));
```

### Exercise 3: Disabled pipeline returns input unchanged

```csharp
var step = Substitute.For<ITransformStep>();

var options = Options.Create(new TransformOptions { Enabled = false });
var pipeline = new TransformPipeline(
    new[] { step }, options, NullLogger<TransformPipeline>.Instance);

var result = await pipeline.ExecuteAsync("{\"id\":1}", "application/json");

Assert.That(result.Payload, Is.EqualTo("{\"id\":1}"));
Assert.That(result.StepsApplied, Is.EqualTo(0));
await step.DidNotReceive()
    .ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>());
```

### Exercise 4: Payload exceeding max size throws

```csharp
var options = Options.Create(new TransformOptions { MaxPayloadSizeBytes = 10 });
var pipeline = new TransformPipeline(
    Array.Empty<ITransformStep>(), options, NullLogger<TransformPipeline>.Instance);

var largePayload = new string('x', 50);

Assert.ThrowsAsync<InvalidOperationException>(
    () => pipeline.ExecuteAsync(largePayload, "text/plain"));
```

### Exercise 5: Step failure with StopOnStepFailure = false continues

```csharp
var failingStep = Substitute.For<ITransformStep>();
failingStep.Name.Returns("Failing");
failingStep.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
    .ThrowsAsync(new InvalidOperationException("step error"));

var goodStep = Substitute.For<ITransformStep>();
goodStep.Name.Returns("Good");
goodStep.ExecuteAsync(Arg.Any<TransformContext>(), Arg.Any<CancellationToken>())
    .Returns(ci => ci.Arg<TransformContext>().WithPayload("done"));

var options = Options.Create(new TransformOptions { StopOnStepFailure = false });
var pipeline = new TransformPipeline(
    new[] { failingStep, goodStep }, options, NullLogger<TransformPipeline>.Instance);

var result = await pipeline.ExecuteAsync("input", "text/plain");

Assert.That(result.Payload, Is.EqualTo("done"));
Assert.That(result.StepsApplied, Is.EqualTo(1));
```

---

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial16/Lab.cs`](../tests/TutorialLabs/Tutorial16/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial16.Lab"
```

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial16/Exam.cs`](../tests/TutorialLabs/Tutorial16/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial16.Exam"
```

---

**Previous: [← Tutorial 15 — Message Translator](15-message-translator.md)** | **Next: [Tutorial 17 — Normalizer →](17-normalizer.md)**
