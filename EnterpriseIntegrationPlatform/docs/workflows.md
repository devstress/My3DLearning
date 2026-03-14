# Temporal Workflow Documentation

## Overview

Temporal.io is the workflow orchestration engine at the heart of the Enterprise Integration Platform. Every message processed by the platform flows through a Temporal workflow that coordinates validation, transformation, routing, and delivery as a durable, replayable sequence of activities.

## Workflow Definitions

### Base Workflow

All integration workflows inherit from the `IntegrationWorkflow` base class:

```csharp
public abstract class IntegrationWorkflow
{
    protected ILogger Logger { get; }
    protected ActivitySource ActivitySource { get; }

    public abstract Task<WorkflowResult> ExecuteAsync(
        IntegrationEnvelope envelope,
        WorkflowConfiguration config);
}
```

### Standard Integration Workflow

The default workflow follows a linear pipeline:

```
Receive Envelope → Validate → Transform → Route → Deliver → Complete
```

Each step is a Temporal activity with its own retry policy and timeout.

### Branching Workflow

For integrations requiring content-based decisions:

```
Receive Envelope → Validate → Evaluate Rules
                                    │
                        ┌───────────┼───────────┐
                        ▼           ▼           ▼
                    Transform A  Transform B  Transform C
                        │           │           │
                        ▼           ▼           ▼
                    Deliver A    Deliver B    Deliver C
                        │           │           │
                        └───────────┼───────────┘
                                    ▼
                                Complete
```

### Fan-Out/Fan-In Workflow

For splitting a batch into individual messages, processing in parallel, and aggregating results:

```
Receive Batch → Split into N items
                    │
          ┌─────────┼─────────┐
          ▼         ▼         ▼
       Process   Process   Process
       Item 1   Item 2   Item N
          │         │         │
          └─────────┼─────────┘
                    ▼
             Aggregate Results
                    ▼
                 Deliver
                    ▼
                Complete
```

## Activity Patterns

### Activity Interface

Activities are defined as interfaces decorated with attributes:

```csharp
[ActivityInterface]
public interface IValidationActivities
{
    [Activity]
    Task<ValidationResult> ValidateSchemaAsync(
        IntegrationEnvelope envelope,
        string schemaId);

    [Activity]
    Task<ValidationResult> ValidateBusinessRulesAsync(
        IntegrationEnvelope envelope,
        List<Rule> rules);
}
```

### Activity Types

| Activity Type     | Responsibility                                    | Typical Duration |
|-------------------|---------------------------------------------------|------------------|
| Validate          | Schema and business rule validation               | < 100ms          |
| Transform         | Payload format conversion and field mapping        | 100ms–1s         |
| Route             | Content-based routing rule evaluation              | < 50ms           |
| Enrich            | External data lookup and augmentation              | 500ms–5s         |
| Deliver           | Outbound connector invocation                      | 1s–30s           |
| Notify            | Send notifications (email, webhook, Slack)         | 1s–10s           |
| Store             | Persist data to Cassandra or external storage      | 100ms–1s         |
| Compensate        | Undo a prior activity's side effects               | 1s–30s           |

### Activity Configuration

Each activity execution is configured with:

```csharp
var activityOptions = new ActivityOptions
{
    StartToCloseTimeout = TimeSpan.FromMinutes(2),
    RetryPolicy = new RetryPolicy
    {
        InitialInterval = TimeSpan.FromSeconds(1),
        BackoffCoefficient = 2.0,
        MaximumAttempts = 5,
        MaximumInterval = TimeSpan.FromMinutes(1),
        NonRetryableErrorTypes = new[] { "ValidationException" }
    }
};
```

## Saga Patterns

### Saga Coordinator

For multi-step processes requiring compensation, the saga pattern ensures consistency:

```csharp
public async Task<WorkflowResult> ExecuteSagaAsync(IntegrationEnvelope envelope)
{
    var saga = new SagaBuilder();

    // Step 1: Deliver to System A
    var resultA = await ExecuteActivityAsync<DeliveryResult>(
        () => activities.DeliverToSystemA(envelope));
    saga.AddCompensation(() => activities.CompensateSystemA(resultA));

    // Step 2: Deliver to System B
    var resultB = await ExecuteActivityAsync<DeliveryResult>(
        () => activities.DeliverToSystemB(envelope));
    saga.AddCompensation(() => activities.CompensateSystemB(resultB));

    // Step 3: Deliver to System C (if this fails, compensate A and B)
    try
    {
        await ExecuteActivityAsync<DeliveryResult>(
            () => activities.DeliverToSystemC(envelope));
    }
    catch (ActivityFailureException ex)
    {
        await saga.CompensateAsync(); // Undo Steps 1 and 2
        throw;
    }

    return WorkflowResult.Success();
}
```

### Compensation Strategies

| Failure Scenario          | Compensation Action                                    |
|---------------------------|--------------------------------------------------------|
| HTTP delivery failed      | Send DELETE/reversal to previously delivered systems   |
| File upload failed        | Delete files uploaded to prior SFTP targets            |
| Database write failed     | Execute reversal queries on prior database writes      |
| Partial batch delivery    | Notify receiving systems of incomplete batch           |

## Error Handling

### Error Classification

Errors are classified to determine the appropriate response:

```csharp
public enum ErrorCategory
{
    Transient,      // Retry automatically (network, timeout)
    Permanent,      // Do not retry (validation, auth, schema)
    Infrastructure, // Retry with longer delay (Kafka down, DB connection)
    Business        // Route to human review (rule violation, data quality)
}
```

### Error Flow

```
Activity throws exception
        │
        ▼
Classify error category
        │
   ┌────┴────────────────┐────────────────┐
   ▼                     ▼                ▼
Transient            Permanent         Business
   │                     │                │
   ▼                     ▼                ▼
Retry per            Route to DLQ     Signal for
retry policy         immediately       human review
   │
   ▼
Exhausted? ──Yes──▶ Route to DLQ
   │
   No
   │
   ▼
Retry activity
```

### DLQ Routing

When a message exhausts retries or encounters a permanent error:

```csharp
public async Task RouteToDlqAsync(
    IntegrationEnvelope envelope,
    Exception error,
    ProcessingHistory history)
{
    var dlqMessage = new DlqMessage
    {
        OriginalEnvelope = envelope,
        ErrorType = error.GetType().Name,
        ErrorMessage = error.Message,
        StackTrace = error.StackTrace,
        FailedActivityId = history.CurrentActivityId,
        AttemptCount = history.AttemptCount,
        ProcessingHistory = history.Steps,
        Timestamp = DateTimeOffset.UtcNow
    };

    await brokerProducer.PublishAsync(
        $"{envelope.MessageType}.dlq", dlqMessage);
}
```

## Versioning

### Workflow Versioning Strategy

Temporal supports deterministic workflow versioning for safe deployments:

```csharp
public async Task<WorkflowResult> ExecuteAsync(IntegrationEnvelope envelope)
{
    int version = Workflow.GetVersion("add-enrichment-step", 1, 2);

    await activities.ValidateAsync(envelope);
    await activities.TransformAsync(envelope);

    if (version >= 2)
    {
        await activities.EnrichAsync(envelope); // Added in version 2
    }

    await activities.DeliverAsync(envelope);
    return WorkflowResult.Success();
}
```

### Versioning Rules

1. Never remove or reorder activities in existing workflow code paths.
2. Use `Workflow.GetVersion()` to introduce new steps or change behavior.
3. Old workflow executions continue with the original code path.
4. New workflow executions follow the updated code path.
5. After all old executions complete, legacy version branches can be cleaned up.

## Testing Strategies

### Unit Testing Activities

Activities are tested in isolation with mocked dependencies:

```csharp
[Fact]
public async Task ValidateSchema_ValidPayload_ReturnsSuccess()
{
    var activity = new ValidationActivities(mockSchemaRegistry.Object);
    var envelope = TestEnvelopeBuilder.Create()
        .WithPayload("{\"orderId\": 1}")
        .Build();

    var result = await activity.ValidateSchemaAsync(envelope, "order-schema");

    Assert.True(result.IsValid);
}
```

### Integration Testing Workflows

Workflows are tested using Temporal's test framework:

```csharp
[Fact]
public async Task IntegrationWorkflow_HappyPath_CompletesSuccessfully()
{
    var env = await Temporalio.Testing.WorkflowEnvironment.StartLocalAsync();
    // Register workflow and mock activities
    // Execute workflow with test envelope
    // Assert expected activity calls and final result
}
```

### End-to-End Testing

Full pipeline tests verify the complete flow:

1. Publish a test envelope to the message broker.
2. Verify workflow execution in Temporal.
3. Assert connector delivery to a mock target.
4. Verify audit records in Cassandra.
5. Check OpenTelemetry traces for completeness.
