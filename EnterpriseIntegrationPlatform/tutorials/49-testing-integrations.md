# Tutorial 49 — Testing Integrations

Write unit, contract, integration, and load tests for integration pipelines.

## Learning Objectives

After completing this tutorial you will be able to:

1. Capture published messages with `NatsBrokerEndpoint` for test verification
2. Track messages across multiple topics in a single test
3. Create `IntegrationEnvelope<T>` instances and verify all identity fields
4. Build causation chains and inspect `FaultEnvelope` details
5. Advance a `RoutingSlip` step-by-step through its itinerary

---

## Key Types

```csharp
// src/Contracts/IntegrationEnvelope.cs — the universal message wrapper
public record IntegrationEnvelope<T>
{
    public Guid MessageId { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public string Source { get; init; }
    public string MessageType { get; init; }
    public T Payload { get; init; }
}

// src/Contracts/FaultEnvelope.cs — wraps a failed message
public record FaultEnvelope
{
    public Guid OriginalMessageId { get; init; }
    public string ExceptionType { get; init; }
    public string ExceptionMessage { get; init; }
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `NatsBrokerEndpoint_CapturesPublishedMessages` | MockEndpoint captures published messages |
| 2 | `NatsBrokerEndpoint_TracksMultipleTopics` | Track messages across multiple topics |
| 3 | `IntegrationEnvelope_Create_SetsAllFields` | Envelope factory sets all identity fields |
| 4 | `CausationId_TracksDerivedMessages` | CausationId tracks derived messages |
| 5 | `FaultEnvelope_CapturesOriginalDetails` | FaultEnvelope captures original details |
| 6 | `RoutingSlip_Advance_MovesToNextStep` | RoutingSlip advance moves to next step |

> 💻 [`tests/TutorialLabs/Tutorial49/Lab.cs`](../tests/TutorialLabs/Tutorial49/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial49.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_CausationChain_ThreeGenerationsPublished` | 🟢 Starter | Three-generation causation chain published |
| 2 | `Challenge2_FaultEnvelope_WithException` | 🟡 Intermediate | FaultEnvelope created from exception |
| 3 | `Challenge3_RoutingSlipLifecycle_PublishesEachStep` | 🔴 Advanced | Routing slip lifecycle — publish each step |

> 💻 [`tests/TutorialLabs/Tutorial49/Exam.cs`](../tests/TutorialLabs/Tutorial49/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial49.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial49.ExamAnswers"
```

---

**Previous: [← Tutorial 48](48-notification-use-cases.md)** | **Next: [Tutorial 50 →](50-best-practices.md)**
