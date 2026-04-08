# Tutorial 47 — Saga Compensation

Compensate partial failures in distributed workflows using saga rollback activities.

## Learning Objectives

After completing this tutorial you will be able to:

1. Execute single-step and multi-step saga compensations
2. Detect compensation failures and publish Nack notifications
3. Inspect `IntegrationPipelineResult` failure reasons
4. Verify that saga workflow and activity types exist in the assembly
5. Understand the Temporal-based saga compensation pattern

## Key Types

```csharp
// src/Workflow.Temporal/Workflows/AtomicPipelineWorkflow.cs (simplified)
[Workflow]
public class AtomicPipelineWorkflow
{
    [WorkflowRun]
    public async Task<AtomicPipelineResult> RunAsync(IntegrationPipelineInput input)
    {
        var completedSteps = new List<string>();

        // Step 1: Persist message as Pending
        await Workflow.ExecuteActivityAsync(
            (PipelineActivities act) => act.PersistMessageAsync(input),
            PipelineActivityOptions);
        completedSteps.Add("PersistMessage");

        // Step 2: Log Received lifecycle event
        await Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.LogStageAsync(input.MessageId, input.MessageType, "Received"),
            PipelineActivityOptions);
        completedSteps.Add("LogReceived");

        // Step 3: Validate message
        var validation = await Workflow.ExecuteActivityAsync(
            (IntegrationActivities act) =>
                act.ValidateMessageAsync(input.MessageType, input.PayloadJson),
            ValidationActivityOptions);

        if (!validation.IsValid)
        {
            // Nack path: compensate all completed steps, then Nack
            return await HandleNackWithRollbackAsync(
                input, completedSteps, validation.Reason ?? "Validation failed");
        }

        // Step 4: Update status to Delivered
        await Workflow.ExecuteActivityAsync(
            (PipelineActivities act) =>
                act.UpdateDeliveryStatusAsync(
                    input.MessageId, input.CorrelationId,
                    input.Timestamp, "Delivered"),
            PipelineActivityOptions);

        // Step 5: Publish Ack (only if notifications enabled)
        if (input.NotificationsEnabled)
        {
            await Workflow.ExecuteActivityAsync(
                (PipelineActivities act) =>
                    act.PublishAckAsync(input.MessageId, input.CorrelationId, input.AckSubject),
                PipelineActivityOptions);
        }

        return new AtomicPipelineResult(input.MessageId, true);
    }
}
```

```csharp
// src/Workflow.Temporal/Activities/SagaCompensationActivities.cs
public sealed class SagaCompensationActivities
{
    private readonly ICompensationActivityService _compensationService;
    private readonly IMessageLoggingService _logging;

    [Activity]
    public async Task<bool> CompensateStepAsync(Guid correlationId, string stepName)
    {
        await _logging.LogAsync(correlationId, stepName, $"CompensationStarted:{stepName}");
        var success = await _compensationService.CompensateAsync(correlationId, stepName);
        var stage = success
            ? $"CompensationSucceeded:{stepName}"
            : $"CompensationFailed:{stepName}";
        await _logging.LogAsync(correlationId, stepName, stage);
        return success;
    }
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `CompensateAsync_SingleStep_ReturnsTrue` | Single-step compensation returns true |
| 2 | `CompensateAsync_MultipleSteps_AllReturnTrue` | Multi-step compensation all succeed |
| 3 | `MockCompensation_FailureDetected_NackPublished` | Failure detected — Nack published |
| 4 | `IntegrationPipelineResult_FailureHasReason` | Pipeline result failure has reason |
| 5 | `SagaCompensationWorkflow_ClassExists` | Saga workflow class exists in assembly |
| 6 | `SagaCompensationActivities_ClassExists` | Saga activities class exists in assembly |

> 💻 [`tests/TutorialLabs/Tutorial47/Lab.cs`](../tests/TutorialLabs/Tutorial47/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial47.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_MultiStepCompensation_AllNotified` | 🟢 Starter | Multi-step compensation — all steps notified |
| 2 | `Challenge2_PartialFailure_FailureNotificationPublished` | 🟡 Intermediate | Partial failure notification published |
| 3 | `Challenge3_SagaWorkflowTypes_ExistInAssembly` | 🔴 Advanced | Saga workflow types exist in assembly |

> 💻 [`tests/TutorialLabs/Tutorial47/Exam.cs`](../tests/TutorialLabs/Tutorial47/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial47.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial47.ExamAnswers"
```

---

**Previous: [← Tutorial 46](46-complete-integration.md)** | **Next: [Tutorial 48 →](48-notification-use-cases.md)**
