# Tutorial 15 — Message Translator

Converts a message payload from one format to another while preserving envelope identity (CorrelationId, CausationId chain).

---

## Learning Objectives

1. Understand the Message Translator pattern and how it converts payloads between formats
2. Wire a `IPayloadTransform<TIn, TOut>` into a `MessageTranslator` to perform payload conversion
3. Verify that CorrelationId is preserved and CausationId is set to the source MessageId
4. Override Source and MessageType via `TranslatorOptions` configuration
5. Confirm metadata dictionaries survive translation unchanged
6. Validate that an empty TargetTopic triggers an `InvalidOperationException`

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

## Lab — Guided Practice

> 💻 Run the lab tests to see each Message Translator concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Translate_TransformsPayload_PublishesToTarget` | Core payload transformation and target topic publishing |
| 2 | `Translate_PreservesCorrelationId` | CorrelationId preserved across translation |
| 3 | `Translate_SetsCausationIdToSourceMessageId` | CausationId set to source MessageId |
| 4 | `Translate_OverridesSourceAndMessageType` | Source and MessageType overrides via TranslatorOptions |
| 5 | `Translate_PreservesMetadata` | Metadata dictionary preserved through translation |
| 6 | `Translate_NoTargetTopic_ThrowsInvalidOperation` | Validation throws when TargetTopic is empty |

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial15.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Starter_TypeConversion_StringToInt` | 🟢 Starter | TypeConversion — StringToInt |
| 2 | `Intermediate_MetadataPreservationChain_TwoTranslations` | 🟡 Intermediate | MetadataPreservationChain — TwoTranslations |
| 3 | `Advanced_PreservesSourceWhenNoOverride` | 🔴 Advanced | PreservesSourceWhenNoOverride |

> 💻 [`tests/TutorialLabs/Tutorial15/Exam.cs`](../tests/TutorialLabs/Tutorial15/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial15.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial15.ExamAnswers"
```
---

**Previous: [← Tutorial 14 — Process Manager](14-process-manager.md)** | **Next: [Tutorial 16 — Transform Pipeline →](16-transform-pipeline.md)**
