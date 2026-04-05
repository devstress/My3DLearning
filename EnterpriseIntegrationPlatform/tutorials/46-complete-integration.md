# Tutorial 46 — Complete End-to-End Integration

## What You'll Learn

- How every layer of the EIP platform works together in a single message flow
- The full journey: HTTP POST → Gateway → broker → Temporal workflow → process → deliver → notify
- Each processing stage: validate, transform, route, deliver, acknowledge
- How EIP patterns (Normalizer, Content-Based Router, Channel Adapter) combine
- Connection to UC2 from the notification framework

## The Complete Message Flow

```
 Client                Gateway.Api          Broker           Temporal
   │                      │                   │                 │
   │  POST /api/message   │                   │                 │
   ├─────────────────────▶│                   │                 │
   │                      │  Publish          │                 │
   │                      ├──────────────────▶│                 │
   │  202 Accepted        │                   │  Consume        │
   │◀─────────────────────┤                   ├────────────────▶│
   │                      │                   │                 │
   │                      │                   │    ┌────────────┤
   │                      │                   │    │ Workflow    │
   │                      │                   │    │ Activities: │
   │                      │                   │    │ 1. Validate │
   │                      │                   │    │ 2. Transform│
   │                      │                   │    │ 3. Route    │
   │                      │                   │    │ 4. Deliver  │
   │                      │                   │    │ 5. Notify   │
   │                      │                   │    └────────────┤
   │                      │                   │                 │
```

## Step 1: HTTP POST to Gateway.Api

The client sends an integration message:

```bash
curl -X POST https://localhost:5001/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "source": "OrderSystem",
    "destination": "InventoryService",
    "payload": "<Order><Item>Widget</Item><Qty>10</Qty></Order>",
    "contentType": "application/xml",
    "notificationsEnabled": true
  }'
```

Gateway.Api wraps this in an `IntegrationEnvelope` and publishes to the broker.

## Step 2: Broker Receives and Queues

The message enters the configured broker (Kafka, NATS JetStream, or Pulsar):

```
┌─────────────────────────────────────────┐
│              Message Broker             │
│  ┌───────────────────────────────────┐  │
│  │  integration.inbound  topic/queue │  │
│  │  ┌─────┐ ┌─────┐ ┌─────┐        │  │
│  │  │msg 1│ │msg 2│ │msg 3│ ...     │  │
│  │  └─────┘ └─────┘ └─────┘        │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

Competing Consumers (pipeline workers) pick up messages for processing.

## Step 3: Temporal Workflow Orchestration

The worker starts an `IntegrationPipelineWorkflow` (or `AtomicPipelineWorkflow`
for saga compensation). Temporal manages retries and state.

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

> **Note:** The workflow orchestrates three activity classes: `IntegrationActivities` (validation), `PipelineActivities` (persistence and notifications), and `SagaCompensationActivities` (rollback — see Tutorial 47). Transform and routing are handled by separate pipeline consumers, not by individual workflow activities.

## Step 4: Validate

The validation activity checks schema compliance, required fields, and
content-type consistency. Invalid messages are rejected with a Nack.

## Step 5: Transform (Normalizer Pattern)

The **Normalizer** (EIP pattern) converts the payload to a canonical format.
Here, XML is transformed to JSON:

```
┌──────────────┐     ┌────────────┐     ┌──────────────┐
│  XML Input   │────▶│ Normalizer │────▶│ JSON Output  │
│  <Order>     │     │ XML → JSON │     │ {"item":     │
│  <Item>...   │     │            │     │  "Widget"..} │
└──────────────┘     └────────────┘     └──────────────┘
```

## Step 6: Route (Content-Based Router)

The **Content-Based Router** examines the message content and routes to the
correct destination channel:

```
                     ┌─────────────────────┐
                     │ Content-Based Router │
                     └──────────┬──────────┘
                    ┌───────────┼───────────┐
                    ▼           ▼           ▼
             ┌──────────┐ ┌──────────┐ ┌──────────┐
             │ Inventory│ │ Shipping │ │ Billing  │
             │ Channel  │ │ Channel  │ │ Channel  │
             └──────────┘ └──────────┘ └──────────┘
```

## Step 7: Deliver (Channel Adapter)

The **Channel Adapter** delivers the message to the external system via the connector:

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

## Step 8: Ack Notification (UC2)

With `NotificationsEnabled = true` and a successful delivery, this matches **UC2**
from the notification framework (see [Tutorial 48](48-notification-use-cases.md)):

```
  Delivery Success
       │
       ▼
  Publish Ack ──▶ NATS notification subject
       │
       ▼
  XmlNotificationMapper
       │
       ▼
  <Ack>ok</Ack>
```

## All Layers Working Together

```
┌─────────┐  ┌───────────┐  ┌────────┐  ┌──────────┐  ┌──────────────┐
│  Client  │─▶│ Gateway   │─▶│ Broker │─▶│ Temporal │─▶│ Activities   │
│  (HTTP)  │  │   .Api    │  │        │  │ Workflow │  │ V→T→R→D→N   │
└─────────┘  └───────────┘  └────────┘  └──────────┘  └──────┬───────┘
                                                              │
              ┌───────────────────────────────────────────────┘
              ▼
     ┌─────────────────┐     ┌──────────────────┐
     │ Channel Adapter  │────▶│ External System  │
     │ (HTTP delivery)  │     │ (InventoryService)│
     └─────────────────┘     └──────────────────┘
```

## Scalability Dimension

Each stage can scale independently: Gateway pods handle HTTP ingress, broker
partitions distribute load, multiple Temporal workers run activities in parallel.
The Content-Based Router fans out to destination-specific channels, each with its
own consumer pool.

## Atomicity Dimension

The Temporal workflow provides durable execution — if any activity fails, the
workflow retries or (with `AtomicPipelineWorkflow`) triggers saga compensation.
Ack/Nack notifications close the feedback loop, ensuring the sender knows the
outcome.

## Lab

**Objective:** Trace a complete message through all 8 processing stages, analyze how each stage contributes to **end-to-end atomicity**, and design a pipeline extension.

### Step 1: Trace a Message Through All 8 Stages

Follow a single message from ingestion to delivery:

| Stage | EIP Pattern | Platform Component | Adds to Envelope |
|-------|------------|-------------------|------------------|
| 1. Receive | Messaging Gateway | Gateway.Api | `MessageId`, `Timestamp` |
| 2. Validate | ? | IntegrationActivities | ? |
| 3. Sanitize | ? | InputSanitizer | ? |
| 4. Transform | Message Translator | MessageTranslator | `CausationId` |
| 5. Route | Content-Based Router | ContentBasedRouter | Routing decision |
| 6. Deliver | Channel Adapter | HttpConnector/SftpConnector | Delivery status |
| 7. Persist | Message Store | CassandraMessageStore | ? |
| 8. Notify | Ack/Nack | NotificationPublisher | Notification payload |

Fill in the `?` cells by tracing through the actual source code.

### Step 2: Analyze Failure at Each Stage

For each stage, identify: What happens if it fails? Where does the message go? Is the failure retryable?

| Stage Failure | Retryable? | Recovery Action | Atomicity Impact |
|--------------|-----------|-----------------|-----------------|
| Stage 4 (Transform) fails | Yes (transient) / No (schema) | Retry or DLQ | Stages 1-3 results preserved |
| Stage 6 (Deliver) fails after Stage 7 (Persist) succeeds | ? | ? | ? |

What is the worst-case scenario for **atomicity** — which combination of stage success/failure creates the hardest recovery?

### Step 3: Design a Pipeline Extension

Add a sixth step: "Audit Logging" that writes every message to a compliance store. Where in the pipeline would you insert it?

- Before routing (Stage 4.5)? — captures the canonical message before routing decisions
- After delivery (Stage 6.5)? — captures the delivery outcome
- As a parallel branch from Stage 2? — captures even messages that fail validation

Justify your choice based on **atomicity** and **compliance** requirements.

## Exam

1. The HTTP connector (Stage 6) returns HTTP 503. How does the platform maintain **end-to-end atomicity**?
   - A) The message is lost
   - B) Temporal's retry policy retries the delivery activity; if all retries fail, the message is Nack'd (UC3), routed to the DLQ with full context, and the originating system is notified of the failure — every stage's work is either committed or compensated
   - C) The workflow restarts from Stage 1
   - D) The connector silently drops the message

2. Why are the 8 stages separated into distinct activities rather than one monolithic handler?
   - A) .NET requires separate classes
   - B) Each stage is an independent filter with its own retry policy, scaling characteristics, and failure handling — this Pipes and Filters architecture enables independent optimization and ensures a failure in one stage doesn't require re-executing all stages
   - C) Monolithic handlers are not supported by Temporal
   - D) Eight stages are required by the EIP book

3. What is the most challenging **atomicity** scenario in the complete pipeline?
   - A) All stages succeed
   - B) Stage 6 (Deliver) succeeds but Stage 7 (Persist) fails — the external system received the message but the platform has no record; compensation requires checking the external system's state and reconciling, which cannot be fully automated
   - C) Stage 1 (Receive) fails
   - D) Stage 8 (Notify) fails

**Previous: [← Tutorial 45](45-performance-profiling.md)** | **Next: [Tutorial 47 →](47-saga-compensation.md)**
