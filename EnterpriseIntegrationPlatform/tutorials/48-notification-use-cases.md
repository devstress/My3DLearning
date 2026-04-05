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

public class XmlNotificationMapper : INotificationMapper
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

## Exercises

1. Design a UC6: the notification publish itself fails (NATS unavailable). How
   should the pipeline handle this? Should it retry, DLQ, or silently drop?

2. Implement a `JsonNotificationMapper` as an alternative to `XmlNotificationMapper`.
   What changes are needed to make the mapper configurable per integration?

3. Write a test that verifies UC4→UC2 transition: disable the feature flag,
   confirm no Ack is published, re-enable, and confirm Ack resumes.

**Previous: [← Tutorial 47](47-saga-compensation.md)** | **Next: [Tutorial 49 →](49-testing-integrations.md)**
