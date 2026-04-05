# Tutorial 42 — Configuration

## What You'll Learn

- How `IConfigurationStore` provides versioned, runtime-updatable configuration
- `IFeatureFlagService` for toggling features with variants, rollout, and tenant targeting
- `InMemoryConfigurationStore` with versioning for development and testing
- `FeatureFlag` with multi-variant support and gradual rollout
- `ConfigurationChangeNotifier` for broadcasting updates to running services via `IObservable<T>`
- `EnvironmentOverrideProvider` and `NotificationFeatureFlags` for notification toggle

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
    Task<ConfigurationEntry?> GetAsync(string key, string environment = "default", CancellationToken ct = default);
    Task<ConfigurationEntry> SetAsync(ConfigurationEntry entry, CancellationToken ct = default);
    Task<bool> DeleteAsync(string key, string environment = "default", CancellationToken ct = default);
    Task<IReadOnlyList<ConfigurationEntry>> ListAsync(string? environment = null, CancellationToken ct = default);
    IObservable<ConfigurationChange> WatchAsync();
}

public sealed record ConfigurationEntry(
    string Key, string Value, string Environment = "default",
    int Version = 1, DateTimeOffset LastModified = default,
    string? ModifiedBy = null);
```

Every `SetAsync` increments the `Version` and preserves the previous value. `InMemoryConfigurationStore` uses a `ConcurrentDictionary` for version tracking. `WatchAsync` returns an `IObservable<ConfigurationChange>` that broadcasts changes as they occur.

### IFeatureFlagService

```csharp
// src/Configuration/IFeatureFlagService.cs
public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string name, string? tenantId = null, CancellationToken ct = default);
    Task<string?> GetVariantAsync(string name, string variantKey, CancellationToken ct = default);
    Task<FeatureFlag?> GetAsync(string name, CancellationToken ct = default);
    Task SetAsync(FeatureFlag flag, CancellationToken ct = default);
    Task<bool> DeleteAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<FeatureFlag>> ListAsync(CancellationToken ct = default);
}
```

### FeatureFlag

```csharp
// src/Configuration/FeatureFlag.cs
public sealed record FeatureFlag
{
    public required string Name { get; init; }
    public bool IsEnabled { get; init; }
    public Dictionary<string, string> Variants { get; init; } = new();
    public int RolloutPercentage { get; init; } = 100;
    public IReadOnlyList<string> TargetTenants { get; init; } = Array.Empty<string>();
}
```

| Property | Purpose |
|----------|---------|
| `IsEnabled` | Master toggle — `false` disables for everyone |
| `Variants` | Named variant keys mapped to values for A/B testing |
| `RolloutPercentage` | Gradual rollout — 25 means 25% of traffic |
| `TargetTenants` | Tenant IDs that always get the feature |

### ConfigurationChangeNotifier

```csharp
// src/Configuration/ConfigurationChangeNotifier.cs
public sealed class ConfigurationChangeNotifier : IObservable<ConfigurationChange>
{
    public void Publish(ConfigurationChange change);
    public IDisposable Subscribe(IObserver<ConfigurationChange> observer);
}

public sealed record ConfigurationChange(
    string Key, string? Value, string Environment,
    ConfigurationChangeType ChangeType, DateTimeOffset Timestamp);

public enum ConfigurationChangeType { Created, Updated, Deleted }
```

Services subscribe via `Subscribe` at startup, receiving an `IDisposable` to unsubscribe later. When a configuration value is updated, the notifier broadcasts the change via `Publish` so services can reload without restarting.

### EnvironmentOverrideProvider

The `EnvironmentOverrideProvider` reads environment variables using the convention `EIP__Key__SubKey` (double underscore as separator). Environment variables take precedence over store values, supporting per-environment overrides without modifying the store.

### NotificationFeatureFlags

```csharp
// src/Configuration/NotificationFeatureFlags.cs
public static class NotificationFeatureFlags
{
    public const string NotificationsEnabled = "notifications.enabled";
}
```

This flag controls whether the platform sends notifications. Toggleable per tenant via `TargetTenants`.

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
