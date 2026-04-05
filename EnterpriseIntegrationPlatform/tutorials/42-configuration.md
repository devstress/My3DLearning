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
public sealed record FeatureFlag(
    string Name,
    bool IsEnabled = false,
    Dictionary<string, string>? Variants = null,
    int RolloutPercentage = 100,
    List<string>? TargetTenants = null)
{
    public Dictionary<string, string> Variants { get; init; } = Variants ?? new();
    public List<string> TargetTenants { get; init; } = TargetTenants ?? [];
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
public sealed class ConfigurationChangeNotifier : IObservable<ConfigurationChange>, IDisposable
{
    public void Publish(ConfigurationChange change);
    public IDisposable Subscribe(IObserver<ConfigurationChange> observer);
}

public sealed record ConfigurationChange(
    string Key, string Environment,
    ConfigurationChangeType ChangeType,
    string? OldValue, string? NewValue,
    DateTimeOffset Timestamp);

public enum ConfigurationChangeType { Created, Updated, Deleted }
```

Services subscribe via `Subscribe` at startup, receiving an `IDisposable` to unsubscribe later. When a configuration value is updated, the notifier broadcasts the change via `Publish` so services can reload without restarting.

### EnvironmentOverrideProvider

The `EnvironmentOverrideProvider` reads environment variables using the convention `EIP__Key__SubKey` (double underscore as separator). Environment variables take precedence over store values, supporting per-environment overrides without modifying the store.

### NotificationFeatureFlags

```csharp
// src/Activities/NotificationFeatureFlags.cs
public static class NotificationFeatureFlags
{
    public const string NotificationsEnabled = "Notifications.Enabled";
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

## Lab

**Objective:** Design feature flags with percentage rollouts, trace configuration change propagation, and analyze how environment overrides support **scalable** multi-environment deployments.

### Step 1: Design a Feature Flag with Gradual Rollout

Design a feature flag for a new routing algorithm:

```csharp
var flag = new FeatureFlag
{
    Name = "new-routing-algorithm",
    DefaultVariant = "v1",
    Variants = ["v1", "v2"],
    Rules = [
        new FeatureFlagRule
        {
            TenantId = "tenant-beta",    // Always enabled for beta testers
            Variant = "v2",
            Percentage = 100
        },
        new FeatureFlagRule
        {
            Variant = "v2",
            Percentage = 10              // 10% of all other traffic
        }
    ]
};
```

Open `src/Configuration/` and trace: How does the platform evaluate which variant to apply? How does percentage-based rollout work — is it random per message or deterministic per tenant?

### Step 2: Trace Configuration Change Propagation

A configuration change updates the retry count from 3 to 5. Trace the flow:

```
1. Operator updates config → IConfigurationStore.SetAsync("retry.maxAttempts", "5")
2. ConfigurationChangeNotifier detects the change
3. All subscribed components receive the notification
4. ExponentialBackoffRetryPolicy reloads the new value
5. Next message uses MaxAttempts = 5
```

What is the propagation delay? What happens to messages already being retried with the old value? Is this an **atomicity** concern?

### Step 3: Analyze Environment Override Scalability

Why does the platform use `EnvironmentOverrideProvider` with the `EIP__` prefix convention?

| Environment | Override Example | Use Case |
|------------|-----------------|----------|
| Development | `EIP__Broker__Type=InMemory` | Use in-memory broker for local dev |
| Staging | `EIP__Retry__MaxAttempts=10` | More aggressive retry for testing |
| Production | `EIP__Throttle__Rate=1000` | Production rate limits |

How does this enable **scalable** multi-environment deployments without changing code or configuration files?

## Exam

1. Why does the platform use configuration change notification rather than reading config on every message?
   - A) Reading configuration is too slow
   - B) Reading config on every message would create a hot path to the configuration store — potentially millions of reads/second; change notification pushes updates only when values change, reducing load by orders of magnitude
   - C) The configuration store doesn't support reads
   - D) Notifications are required by .NET

2. How do feature flags with percentage rollouts support **safe scalability** of new features?
   - A) They make features faster
   - B) Gradual rollout (10% → 50% → 100%) limits the blast radius of bugs — if the new algorithm causes failures, only a percentage of traffic is affected, enabling rapid rollback without impacting all tenants
   - C) Percentage rollouts are required for production
   - D) Feature flags reduce memory usage

3. Why does the `EIP__` environment variable prefix convention support **multi-environment scalability**?
   - A) The prefix is shorter than other options
   - B) Environment variables override configuration store values per deployment — the same code artifact deploys to dev, staging, and production with different behavior controlled by environment, eliminating configuration file management across environments
   - C) The .NET runtime requires specific prefixes
   - D) The prefix prevents name collisions with system variables

---

**Previous: [← Tutorial 41 — OpenClaw Web UI](41-openclaw-web.md)** | **Next: [Tutorial 43 — Kubernetes Deployment →](43-kubernetes-deployment.md)**
