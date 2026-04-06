# Tutorial 15 — Message Translator

Converts a message payload from one format to another while preserving envelope identity (CorrelationId, CausationId chain).

---

## Key Types

```csharp
// src/Processing.Translator/IMessageTranslator.cs
public interface IMessageTranslator<TIn, TOut>
{
    Task<TranslationResult<TOut>> TranslateAsync(
        IntegrationEnvelope<TIn> source,
        CancellationToken cancellationToken = default);
}

// src/Processing.Translator/IPayloadTransform.cs
public interface IPayloadTransform<TIn, TOut>
{
    TOut Transform(TIn source);
}

// src/Processing.Translator/FuncPayloadTransform.cs
public sealed class FuncPayloadTransform<TIn, TOut> : IPayloadTransform<TIn, TOut>
{
    public FuncPayloadTransform(Func<TIn, TOut> transform) { ... }
    public TOut Transform(TIn source) => _transform(source);
}

// src/Processing.Translator/TranslationResult.cs
public sealed record TranslationResult<TOut>(
    IntegrationEnvelope<TOut> TranslatedEnvelope,
    Guid SourceMessageId,
    string TargetTopic);

// src/Processing.Translator/FieldMapping.cs
public sealed record FieldMapping
{
    public required string SourcePath { get; init; }
    public required string TargetPath { get; init; }
    public string? StaticValue { get; init; }
}
```

---

## Exercises

### 1. Basic translation — string to string

```csharp
var transform = new FuncPayloadTransform<string, string>(s => s.ToUpperInvariant());

var options = Options.Create(new TranslatorOptions
{
    TargetTopic = "translated-topic",
});

var translator = new MessageTranslator<string, string>(
    transform, producer, options,
    NullLogger<MessageTranslator<string, string>>.Instance);

var source = IntegrationEnvelope<string>.Create(
    "hello world", "SourceService", "greeting.event");

var result = await translator.TranslateAsync(source);

Assert.That(result.TranslatedEnvelope.Payload, Is.EqualTo("HELLO WORLD"));
Assert.That(result.TargetTopic, Is.EqualTo("translated-topic"));
Assert.That(result.SourceMessageId, Is.EqualTo(source.MessageId));
```

### 2. CorrelationId is preserved across translation

```csharp
var transform = new FuncPayloadTransform<string, string>(s => s);

var translator = new MessageTranslator<string, string>(
    transform, producer, options,
    NullLogger<MessageTranslator<string, string>>.Instance);

var source = IntegrationEnvelope<string>.Create(
    "data", "Service", "event.type");

var result = await translator.TranslateAsync(source);

Assert.That(result.TranslatedEnvelope.CorrelationId, Is.EqualTo(source.CorrelationId));
```

### 3. CausationId set to source MessageId

```csharp
var source = IntegrationEnvelope<string>.Create(
    "data", "Service", "event.type");

var result = await translator.TranslateAsync(source);

Assert.That(result.TranslatedEnvelope.CausationId, Is.EqualTo(source.MessageId));
Assert.That(result.TranslatedEnvelope.MessageId, Is.Not.EqualTo(source.MessageId));
```

### 4. TargetMessageType override changes MessageType

```csharp
var options = Options.Create(new TranslatorOptions
{
    TargetTopic = "output-topic",
    TargetMessageType = "translated.event",
});

var translator = new MessageTranslator<string, string>(
    transform, producer, options,
    NullLogger<MessageTranslator<string, string>>.Instance);

var source = IntegrationEnvelope<string>.Create(
    "data", "Service", "original.event");

var result = await translator.TranslateAsync(source);

Assert.That(result.TranslatedEnvelope.MessageType, Is.EqualTo("translated.event"));
```

### 5. No TargetTopic configured — throws

```csharp
var options = Options.Create(new TranslatorOptions
{
    TargetTopic = "",
});

var translator = new MessageTranslator<string, string>(
    transform, producer, options,
    NullLogger<MessageTranslator<string, string>>.Instance);

var source = IntegrationEnvelope<string>.Create(
    "data", "Service", "event.type");

Assert.ThrowsAsync<InvalidOperationException>(
    () => translator.TranslateAsync(source));
```

---

## Lab

> 💻 [`tests/TutorialLabs/Tutorial15/Lab.cs`](../tests/TutorialLabs/Tutorial15/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial15.Lab"
```

## Exam

> 💻 [`tests/TutorialLabs/Tutorial15/Exam.cs`](../tests/TutorialLabs/Tutorial15/Exam.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial15.Exam"
```

---

**Previous: [← Tutorial 14 — Process Manager](14-process-manager.md)** | **Next: [Tutorial 16 — Transform Pipeline →](16-transform-pipeline.md)**
