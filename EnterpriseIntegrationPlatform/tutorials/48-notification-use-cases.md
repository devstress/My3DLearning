# Tutorial 48 — Notification Use Cases

Implement notification use cases with Ack/Nack, feature flags, and priority routing.

## Learning Objectives

After completing this tutorial you will be able to:

1. Validate messages and publish Ack on success or Nack on failure
2. Inspect `MessageValidationResult` success and failure properties
3. Log notification activity without errors
4. Wire a full notification flow: validate → log → publish

## Key Types

```csharp
public interface INotificationMapper
{
    string MapAck(Guid messageId, Guid correlationId);
    string MapNack(Guid messageId, Guid correlationId, string errorMessage);
}

public sealed class XmlNotificationMapper : INotificationMapper
{
    public string MapAck(Guid messageId, Guid correlationId)
        => "<Ack>ok</Ack>";

    public string MapNack(Guid messageId, Guid correlationId, string errorMessage)
        => $"<Nack>not ok because of {SecurityElement.Escape(errorMessage)}</Nack>";
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Validate_Success_PublishesAck` | Successful validation publishes Ack |
| 2 | `Validate_Failure_PublishesNack` | Failed validation publishes Nack |
| 3 | `LogAsync_CompletesWithoutError` | Log activity completes without error |
| 4 | `MessageValidationResult_Success_HasExpectedValues` | Validation result success properties |
| 5 | `MessageValidationResult_Failure_HasReasonAndInvalid` | Validation result failure reason |
| 6 | `FullNotificationFlow_ValidateLogPublish` | Full notification flow end-to-end |

> 💻 [`tests/TutorialLabs/Tutorial48/Lab.cs`](../tests/TutorialLabs/Tutorial48/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial48.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_ConditionalAckNack_CorrectTopics` | 🟢 Starter | Conditional Ack/Nack to correct topics |
| 2 | `Challenge2_BatchNotification_MultipleMessages` | 🟡 Intermediate | Batch notification with multiple messages |
| 3 | `Challenge3_PersistenceActivity_SaveAndUpdate` | 🔴 Advanced | Persistence activity save and update |

> 💻 [`tests/TutorialLabs/Tutorial48/Exam.cs`](../tests/TutorialLabs/Tutorial48/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial48.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial48.ExamAnswers"
```

---

**Previous: [← Tutorial 47](47-saga-compensation.md)** | **Next: [Tutorial 49 →](49-testing-integrations.md)**
