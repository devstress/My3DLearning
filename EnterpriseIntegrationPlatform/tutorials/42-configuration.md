# Tutorial 42 — Configuration

## What You'll Learn

- How `IConfigurationStore` provides versioned, runtime-updatable configuration
- `IFeatureFlagService` for toggling features with variants, rollout, and tenant targeting
- `InMemoryConfigurationStore` with versioning for development and testing
- `FeatureFlag` with multi-variant support and gradual rollout
- `ConfigurationChangeNotifier` for broadcasting updates to running services
- `EnvironmentOverrideProvider` and `NotificationFeatureFlags` for Ack/Nack toggle

---

## EIP Pattern: Control Bus

> *"Use a Control Bus to manage an enterprise integration system. The Control Bus uses the same messaging mechanism used by the application data, but uses separate channels to transmit configuration and control data."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  ┌──────────────────┐     ┌──────────────────┐
  │  Admin API       │────▶│  Configuration   │
  │  (update config) │     │  Store           │
  └──────────────────┘     └───────┬──────────┘
                                   │ notify
                    ┌──────────────┼──────────────┐
                    ▼              ▼              ▼
              ┌──────────┐  ┌──────────┐  ┌──────────┐
              │ Service  │  │ Service  │  │ Service  │
              │ A        │  │ B        │  │ C        │
              └──────────┘  └──────────┘  └──────────┘
```

Configuration changes flow through the store and notifier to all running services. No restart is required — services react to change notifications in real time.

---

## Platform Implementation

### IConfigurationStore

```csharp
// src/Configuration/IConfigurationStore.cs
public interface IConfigurationStore
{
    Task<ConfigurationEntry?> GetAsync(string key, CancellationToken ct);
    Task SetAsync(string key, string value, CancellationToken ct);
    Task<IReadOnlyList<ConfigurationEntry>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<ConfigurationEntry>> GetHistoryAsync(string key, CancellationToken ct);
}

public sealed record ConfigurationEntry(
    string Key, string Value, int Version,
    DateTimeOffset UpdatedAt, string? UpdatedBy);
```

Every `SetAsync` increments the `Version` and preserves the previous value. `InMemoryConfigurationStore` uses a `ConcurrentDictionary` for version history, queryable via `GetHistoryAsync`.

### IFeatureFlagService

```csharp
// src/Configuration/IFeatureFlagService.cs
public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string flagName, CancellationToken ct);
    Task<string?> GetVariantAsync(string flagName, string? tenantId, CancellationToken ct);
    Task<IReadOnlyList<FeatureFlag>> GetAllFlagsAsync(CancellationToken ct);
}
```

### FeatureFlag

```csharp
// src/Configuration/FeatureFlag.cs
public sealed class FeatureFlag
{
    public required string Name { get; init; }
    public bool IsEnabled { get; init; } = false;
    public IReadOnlyList<string>? Variants { get; init; }
    public double RolloutPercentage { get; init; } = 100.0;
    public IReadOnlyList<string>? TargetTenants { get; init; }
}
```

| Property | Purpose |
|----------|---------|
| `IsEnabled` | Master toggle — `false` disables for everyone |
| `Variants` | Named variants for A/B testing |
| `RolloutPercentage` | Gradual rollout — 25.0 means 25% of traffic |
| `TargetTenants` | Tenant IDs that always get the feature |

### ConfigurationChangeNotifier

```csharp
// src/Configuration/ConfigurationChangeNotifier.cs
public sealed class ConfigurationChangeNotifier
{
    public event Func<ConfigurationChange, Task>? OnChange;
    public async Task NotifyAsync(ConfigurationChange change)
    {
        if (OnChange is not null) await OnChange.Invoke(change);
    }
}

public sealed record ConfigurationChange(string Key, string? OldValue, string NewValue, int NewVersion);
```

Services subscribe to `OnChange` at startup. When a configuration value is updated, the notifier broadcasts the change so services can reload without restarting.

### EnvironmentOverrideProvider

The `EnvironmentOverrideProvider` reads environment variables using the convention `EIP__Key__SubKey` (double underscore as separator). Environment variables take precedence over store values, supporting per-environment overrides without modifying the store.

### NotificationFeatureFlags

```csharp
// src/Configuration/NotificationFeatureFlags.cs
public static class NotificationFeatureFlags
{
    public const string AckNotifications = "notifications.ack.enabled";
    public const string NackNotifications = "notifications.nack.enabled";
}
```

These flags control whether the platform sends notifications when messages are Acked or Nacked. Toggleable per tenant via `TargetTenants`.

---

## Scalability Dimension

The configuration store is **read-heavy** — services read on startup and on change notifications. The `ConfigurationChangeNotifier` uses pub/sub so all replicas receive updates simultaneously, preventing configuration drift. Production uses a distributed store (Redis, database) with local caching.

---

## Atomicity Dimension

Configuration updates are **versioned** — each `SetAsync` is atomic and creates a new version. The version history supports **rollback** by re-setting to a previous value. Feature flags use `RolloutPercentage` with deterministic hashing to ensure consistent behavior per entity.

---

## Exercises

1. Design a feature flag that enables a new routing algorithm for 10% of traffic, always enables it for `"tenant-beta"`, and offers variants `"v1"` and `"v2"`.

2. A configuration change is made to update the retry count from 3 to 5. Trace the flow through `IConfigurationStore`, `ConfigurationChangeNotifier`, and the retry framework.

3. Why does the platform use `EnvironmentOverrideProvider` in addition to the configuration store? When would an environment override be preferable?

---

**Previous: [← Tutorial 41 — OpenClaw Web UI](41-openclaw-web.md)**
