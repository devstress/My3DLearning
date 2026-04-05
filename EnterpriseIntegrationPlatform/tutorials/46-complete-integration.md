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

The message enters the configured broker (Kafka, RabbitMQ, or NATS):

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
public class IntegrationPipelineWorkflow
{
    public async Task<PipelineResult> RunAsync(IntegrationPipelineInput input)
    {
        var validated = await ExecuteActivity<ValidateActivity>(input);
        var transformed = await ExecuteActivity<TransformActivity>(validated);
        var routed = await ExecuteActivity<RouteActivity>(transformed);
        var delivered = await ExecuteActivity<DeliverActivity>(routed);
        if (input.NotificationsEnabled)
            await ExecuteActivity<NotifyActivity>(delivered);
        return delivered.Result;
    }
}
```

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

## Exercises

1. Trace a message through all 8 steps using the Admin.Api dashboard. What
   metadata does each step add to the `IntegrationEnvelope`?

2. What happens if the Channel Adapter returns HTTP 503? How does Temporal's
   retry policy interact with the Nack notification (UC3)?

3. Modify the workflow to add a sixth step: audit logging. Where in the
   pipeline would you insert it, and why?

**Previous: [← Tutorial 45](45-performance-profiling.md)** | **Next: [Tutorial 47 →](47-saga-compensation.md)**
