# Tutorial 48 — Notification Use Cases

## What You'll Learn

- The 5 notification use cases and how the Channel Adapter (EIP pattern) drives them
- How `INotificationMapper` and `XmlNotificationMapper` format Ack/Nack messages
- The role of `NotificationFeatureFlags` and `IFeatureFlagService` toggle
- How `NatsNotificationActivityService` publishes notifications
- How `IntegrationPipelineInput.NotificationsEnabled` controls per-message behavior
- ASCII diagrams for each use case

## Key Components

```
┌───────────────────────────────────────────────────────────┐
│                  Notification Stack                        │
│                                                           │
│  IntegrationPipelineInput.NotificationsEnabled  (per-msg) │
│  NotificationFeatureFlags.NotificationsEnabled   (global)  │
│  IFeatureFlagService                            (toggle)  │
│  INotificationMapper / XmlNotificationMapper    (format)  │
│  NatsNotificationActivityService                (publish) │
└───────────────────────────────────────────────────────────┘
```

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

## UC1: Backward Compatible — No Notifications

**Scenario**: Existing integration, `NotificationsEnabled = false`.

```
  Channel Adapter delivers message
       │
       ▼
  Delivery succeeds (or fails)
       │
       ▼
  NotificationsEnabled = false
       │
       ▼
  ┌─────────────────────────┐
  │  No Ack/Nack published  │
  │  (backward compatible)  │
  └─────────────────────────┘
```

Existing integrations that predate the notification feature continue to
operate without change. No Ack or Nack is produced.

## UC2: Successful Delivery → Ack

**Scenario**: Channel Adapter delivers successfully, `NotificationsEnabled = true`.

```
  Channel Adapter ──▶ HTTP 200 OK
       │
       ▼
  NotificationsEnabled = true
       │
       ▼
  IFeatureFlagService.IsEnabled("Notifications.Enabled") = true
       │
       ▼
  NatsNotificationActivityService.PublishAckAsync()
       │
       ▼
  XmlNotificationMapper.MapAck(messageId, correlationId)
       │
       ▼
  NATS ◀── "<Ack>ok</Ack>"
```

The sender receives confirmation that the message was delivered and processed.

## UC3: Failed Delivery → Nack

**Scenario**: Channel Adapter times out or returns error, `NotificationsEnabled = true`.

```
  Channel Adapter ──▶ HTTP 503 / Timeout
       │
       ▼
  NotificationsEnabled = true
       │
       ▼
  IFeatureFlagService.IsEnabled("Notifications.Enabled") = true
       │
       ▼
  NatsNotificationActivityService.PublishNackAsync()
       │
       ▼
  XmlNotificationMapper.MapNack(messageId, correlationId, "Connection timed out")
       │
       ▼
  NATS ◀── "<Nack>not ok because of Connection timed out</Nack>"
```

The sender learns the delivery failed and can take corrective action.

## UC4: Ack Skipped by Feature Flag

**Scenario**: Same as UC2, but `Notifications.Enabled` feature flag is `false`.

```
  Channel Adapter ──▶ HTTP 200 OK
       │
       ▼
  NotificationsEnabled = true  (per-message)
       │
       ▼
  IFeatureFlagService.IsEnabled("Notifications.Enabled") = false  (global)
       │
       ▼
  ┌──────────────────────────────┐
  │  Ack SKIPPED                 │
  │  (feature flag disabled)     │
  └──────────────────────────────┘

  Re-enable feature flag:
  IFeatureFlagService.SetEnabled("Notifications.Enabled", true)
       │
       ▼
  Resumes UC2 behavior ──▶ Ack published
```

This allows operators to temporarily disable notifications globally during
maintenance windows without changing individual integration configurations.

## UC5: Nack Skipped by Feature Flag

**Scenario**: Same as UC3, but `Notifications.Enabled` feature flag is `false`.

```
  Channel Adapter ──▶ HTTP 503 / Timeout
       │
       ▼
  NotificationsEnabled = true  (per-message)
       │
       ▼
  IFeatureFlagService.IsEnabled("Notifications.Enabled") = false  (global)
       │
       ▼
  ┌──────────────────────────────┐
  │  Nack SKIPPED                │
  │  (feature flag disabled)     │
  └──────────────────────────────┘

  Re-enable feature flag:
  IFeatureFlagService.SetEnabled("Notifications.Enabled", true)
       │
       ▼
  Resumes UC3 behavior ──▶ Nack published
```

## Decision Flow Summary

```
  Delivery Complete
       │
       ├── NotificationsEnabled = false? ──▶ UC1: No notification
       │
       ├── Feature flag disabled? ──▶ UC4/UC5: Notification skipped
       │
       ├── Delivery succeeded? ──▶ UC2: Publish Ack
       │
       └── Delivery failed?    ──▶ UC3: Publish Nack
```

```csharp
// Conceptual pseudocode — the actual notification logic lives inside
// IntegrationPipelineWorkflow and AtomicPipelineWorkflow (see Tutorials 46–47).
// This example illustrates the decision flow for reference:
public class NotificationDecisionService
{
    public async Task HandleDeliveryResultAsync(
        DeliveryResult result, IntegrationPipelineInput input)
    {
        if (!input.NotificationsEnabled) return;              // UC1
        if (!await _featureFlags.IsEnabledAsync(
            NotificationFeatureFlags.NotificationsEnabled)) return; // UC4/UC5

        if (result.Success)
            await _notificationService.PublishAckAsync(       // UC2
                _mapper.MapAck(input.MessageId, input.CorrelationId));
        else
            await _notificationService.PublishNackAsync(      // UC3
                _mapper.MapNack(input.MessageId, input.CorrelationId, result.ErrorMessage));
    }
}
```

## Scalability Dimension

Notifications are published to NATS, which supports fan-out to multiple
subscribers. As integration volume grows, notification consumers scale
independently from the pipeline workers that produce them.

## Atomicity Dimension

The two-level toggle (per-message `NotificationsEnabled` + global feature flag)
provides fine-grained control over the Ack/Nack feedback loop. Feature flags
enable instant, zero-deployment changes to notification behavior.

## Lab

> 💻 **Runnable lab:** [`tests/TutorialLabs/Tutorial48/Lab.cs`](../tests/TutorialLabs/Tutorial48/Lab.cs)

**Objective:** Design notification failure handling, analyze mapper configurability for **scalable** multi-format notification delivery, and trace feature flag interaction with notification flows.

### Step 1: Design UC6 — Notification Publish Failure

The notification publish itself fails (NATS unavailable). Design the handling strategy:

| Option | Behavior | Trade-off |
|--------|----------|-----------|
| A. Retry | Retry notification publish with backoff | Delays pipeline Ack |
| B. DLQ | Route notification to DLQ | Notification may never be sent |
| C. Silent drop | Log warning, continue pipeline | Originating system doesn't know outcome |
| D. Best-effort + fallback | Try publish, if fails log event + continue | Pipeline isn't blocked, event is recorded |

Which option preserves **pipeline atomicity** while being operationally practical? (hint: the notification is about the outcome, not the outcome itself — the message was already delivered)

### Step 2: Design a Configurable Notification Mapper

The platform uses `XmlNotificationMapper`. Design a `JsonNotificationMapper` alternative:

```json
{
  "notification": {
    "type": "Ack",
    "messageId": "abc-123",
    "correlationId": "xyz-789",
    "timestamp": "2024-01-15T10:30:00Z",
    "source": "EIP.Platform"
  }
}
```

How would you make the mapper configurable per integration (some partners want XML, others want JSON)?

| Configuration | Mapper | Output Format |
|--------------|--------|---------------|
| `PartnerA.NotificationFormat = "XML"` | `XmlNotificationMapper` | XML Ack/Nack |
| `PartnerB.NotificationFormat = "JSON"` | `JsonNotificationMapper` | JSON Ack/Nack |
| Default | `XmlNotificationMapper` | XML (backward compatible) |

### Step 3: Trace Feature Flag Interaction

UC4 (conditional Ack) uses a feature flag `NotificationsEnabled`. Trace the flow:

1. Feature flag enabled → Ack published after delivery
2. Feature flag disabled → No Ack published
3. Flag is toggled mid-processing → What happens to in-flight messages?

Is the feature flag check **atomic** with the notification publish? What race condition could occur if the flag is disabled between the check and the publish?

## Exam

> 💻 **Coding exam:** [`tests/TutorialLabs/Tutorial48/Exam.cs`](../tests/TutorialLabs/Tutorial48/Exam.cs)

Complete the coding challenges in the exam file. Each challenge is a failing test — make it pass by writing the correct implementation inline.

---

**Previous: [← Tutorial 47](47-saga-compensation.md)** | **Next: [Tutorial 49 →](49-testing-integrations.md)**
