# Tutorial 47 — Saga Compensation

Compensate partial failures in distributed workflows using saga rollback activities.

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

## Exercises

### 1. CompensateAsync — ReturnsTrue

```csharp
var svc = new DefaultCompensationActivityService(
    NullLogger<DefaultCompensationActivityService>.Instance);

var result = await svc.CompensateAsync(Guid.NewGuid(), "validate");

Assert.That(result, Is.True);
```

### 2. ICompensationActivityService — InterfaceShape

```csharp
var type = typeof(ICompensationActivityService);

Assert.That(type.IsInterface, Is.True);
Assert.That(type.GetMethod("CompensateAsync"), Is.Not.Null);
```

### 3. SagaCompensationActivities — ClassExists

```csharp
var assembly = typeof(EnterpriseIntegrationPlatform.Workflow.Temporal.TemporalOptions).Assembly;
var type = assembly.GetTypes()
    .FirstOrDefault(t => t.Name == "SagaCompensationActivities");

Assert.That(type, Is.Not.Null);
```

### 4. SagaCompensationWorkflow — ClassExists

```csharp
var assembly = typeof(EnterpriseIntegrationPlatform.Workflow.Temporal.TemporalOptions).Assembly;
var type = assembly.GetTypes()
    .FirstOrDefault(t => t.Name == "SagaCompensationWorkflow");

Assert.That(type, Is.Not.Null);
```

### 5. CompensateAsync — MultipleSteps AllReturnTrue

```csharp
var svc = new DefaultCompensationActivityService(
    NullLogger<DefaultCompensationActivityService>.Instance);

var corrId = Guid.NewGuid();
var r1 = await svc.CompensateAsync(corrId, "persist");
var r2 = await svc.CompensateAsync(corrId, "notify");
var r3 = await svc.CompensateAsync(corrId, "route");

Assert.That(r1, Is.True);
Assert.That(r2, Is.True);
Assert.That(r3, Is.True);
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial47/Lab.cs`](../tests/TutorialLabs/Tutorial47/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial47.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial47/Exam.cs`](../tests/TutorialLabs/Tutorial47/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial47.Exam"
```

---

**Previous: [← Tutorial 46](46-complete-integration.md)** | **Next: [Tutorial 48 →](48-notification-use-cases.md)**
