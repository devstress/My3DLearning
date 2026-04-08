# Tutorial 33 — Security

Sanitize input, encrypt payloads, and validate messages against security threats.

## Learning Objectives

After completing this tutorial you will be able to:

1. Sanitise message payloads by stripping script tags and SQL injection patterns
2. Evaluate whether a string payload is clean using `InputSanitizer.IsClean`
3. Enforce maximum payload size with `PayloadSizeGuard`
4. Wire a sanitise → guard → publish pipeline through a `MockEndpoint`
5. Understand how input validation fits into an integration security model

## Key Types

```csharp
// src/Security/IInputSanitizer.cs
public interface IInputSanitizer
{
    string Sanitize(string input);
    bool IsClean(string input);
}
```

```csharp
// src/Security/IPayloadSizeGuard.cs
public interface IPayloadSizeGuard
{
    void Enforce(string payload);
    void Enforce(byte[] payloadBytes);
}
```

```csharp
// src/Security/PayloadTooLargeException.cs
public sealed class PayloadTooLargeException : Exception
{
    public int ActualBytes { get; }
    public int MaxBytes { get; }
}
```

```csharp
// src/Security/JwtOptions.cs
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public bool ValidateLifetime { get; set; } = true;
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Sanitizer_RemovesScriptTags` | Strip `<script>` tags from payloads |
| 2 | `Sanitizer_RemovesSqlInjection` | Strip SQL injection patterns |
| 3 | `IsClean_DetectsDangerousInput` | Detect dangerous input strings |
| 4 | `PayloadSizeGuard_AllowsUnderLimit` | Payload under size limit passes |
| 5 | `PayloadSizeGuard_RejectsOverLimit` | Payload over size limit is rejected |
| 6 | `SanitizedMessage_PublishedToMockEndpoint` | End-to-end sanitise and publish |

> 💻 [`tests/TutorialLabs/Tutorial33/Lab.cs`](../tests/TutorialLabs/Tutorial33/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial33.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_FullSanitizePipeline_PublishesCleanMessages` | 🟢 Starter | Full sanitise pipeline that publishes clean messages |
| 2 | `Challenge2_ByteArrayPayloadGuard` | 🟡 Intermediate | Byte-array payload size guard |
| 3 | `Challenge3_CombinedSanitizerAndGuard_E2E` | 🔴 Advanced | Combined sanitiser + guard end-to-end |

> 💻 [`tests/TutorialLabs/Tutorial33/Exam.cs`](../tests/TutorialLabs/Tutorial33/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial33.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial33.ExamAnswers"
```

---

**Previous: [← Tutorial 32 — Multi-Tenancy](32-multi-tenancy.md)** | **Next: [Tutorial 34 — HTTP Connector →](34-connector-http.md)**
