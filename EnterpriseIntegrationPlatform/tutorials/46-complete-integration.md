# Tutorial 46 — Complete End-to-End Integration

Wire together multiple EIP patterns into a complete end-to-end integration pipeline.

## Learning Objectives

After completing this tutorial you will be able to:

1. Register and dispatch message handlers with the `Dispatcher`
2. Handle unknown message types gracefully with a NotFound result
3. Publish dispatched results to a `NatsBrokerEndpoint`
4. Invoke services with request-reply semantics via `ServiceActivator`
5. Orchestrate integration pipelines with the `PipelineOrchestrator`

## Key Types

```csharp
// src/Workflow.Temporal/Workflows/IntegrationPipelineWorkflow.cs (simplified)
[Workflow]
public class IntegrationPipelineWorkflow
{
    [WorkflowRun]
    public async Task<IntegrationPipelineResult> RunAsync(IntegrationPipelineInput input)
    {
        // Step 1: Persist message to storage
        await Workflow.ExecuteActivityAsync(
            (PipelineActivities act) => act.PersistMessageAsync(input),
            PipelineActivityOptions);

        // Step 2: Validate message schema and content
        var validation = await Workflow.ExecuteActivityAsync(
            (IntegrationActivities act) =>
                act.ValidateMessageAsync(input.MessageType, input.PayloadJson),
            ValidationActivityOptions);

        if (!validation.IsValid)
        {
            if (input.NotificationsEnabled)
            {
                await Workflow.ExecuteActivityAsync(
                    (PipelineActivities act) =>
                        act.PublishNackAsync(input.MessageId, input.CorrelationId,
                            validation.Reason ?? "Validation failed", input.NackSubject),
                    PipelineActivityOptions);
            }
            return new IntegrationPipelineResult(input.MessageId, false, validation.Reason);
        }

        // Steps 3-4: Transform and Route are handled externally via
        // the Normalizer and Content-Based Router patterns — the workflow
        // publishes to the appropriate channel and downstream consumers
        // handle format conversion and routing decisions.

        // Step 5: Publish success acknowledgment
        if (input.NotificationsEnabled)
        {
            await Workflow.ExecuteActivityAsync(
                (PipelineActivities act) =>
                    act.PublishAckAsync(input.MessageId, input.CorrelationId,
                        input.AckSubject),
                PipelineActivityOptions);
        }

        return new IntegrationPipelineResult(input.MessageId, true);
    }
}
```

```csharp
// src/Connector.Http/HttpConnectorAdapter.cs
public sealed class HttpConnectorAdapter : IConnector
{
    public async Task<ConnectorResult> SendAsync<T>(
        IntegrationEnvelope<T> envelope,
        ConnectorSendOptions options,
        CancellationToken cancellationToken = default)
    {
        // Sends the envelope payload via HTTP to the configured endpoint
        // Returns ConnectorResult with success/failure status
    }
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Dispatcher_RegisterAndDispatch_HandlerInvoked` | Register and dispatch handler |
| 2 | `Dispatcher_UnknownType_ReturnsNotFound` | Unknown type returns NotFound |
| 3 | `Dispatcher_DispatchAndPublish_NatsBrokerEndpointReceives` | Dispatch and publish to broker |
| 4 | `ServiceActivator_InvokeWithReply_PublishesToReplyTopic` | Service activator request-reply |
| 5 | `ServiceActivator_NoReplyTo_NoReplyPublished` | No ReplyTo — no reply published |
| 6 | `PipelineOrchestrator_ProcessAsync_DispatchesToWorkflow` | Pipeline orchestrator dispatches to workflow |
| 7 | `PipelineOrchestrator_MapsAckNackFromOptions` | Pipeline maps Ack/Nack from options |

> 💻 [`tests/TutorialLabs/Tutorial46/Lab.cs`](../tests/TutorialLabs/Tutorial46/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial46.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_FullDispatchToPublish_EndToEnd` | 🟢 Starter | Full dispatch-to-publish end-to-end |
| 2 | `Challenge2_ServiceActivator_RequestReplyFlow` | 🟡 Intermediate | Service activator request-reply flow |
| 3 | `Challenge3_PipelineFailure_HandledGracefully` | 🔴 Advanced | Pipeline failure handled gracefully |

> 💻 [`tests/TutorialLabs/Tutorial46/Exam.cs`](../tests/TutorialLabs/Tutorial46/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial46.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial46.ExamAnswers"
```

---

**Previous: [← Tutorial 45](45-performance-profiling.md)** | **Next: [Tutorial 47 →](47-saga-compensation.md)**
