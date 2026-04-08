# Tutorial 42 — Configuration

Manage platform configuration with environment overrides, feature flags, and hot reload.

## Learning Objectives

After completing this tutorial you will be able to:

1. Set, get, update, and delete configuration values via the config store
2. Observe automatic version increments on configuration updates
3. List configuration entries filtered by environment
4. Evaluate feature flags with tenant-targeted rollout
5. Retrieve feature flag variants and publish routing decisions

## Key Types

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

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `SetAndGet_PublishConfigValueToNatsBrokerEndpoint` | Set and get config value via broker |
| 2 | `UpdateConfig_VersionIncrements_PublishChange` | Config update increments version |
| 3 | `DeleteConfig_PublishDeletionNotification` | Delete config publishes notification |
| 4 | `ListByEnvironment_PublishFilteredEntries` | List config entries filtered by environment |
| 5 | `FeatureFlag_SetAndEvaluate_PublishDecision` | Feature flag evaluation and publish |
| 6 | `FeatureFlag_TargetTenant_PublishRouting` | Feature flag tenant-targeted routing |
| 7 | `FeatureFlag_GetVariant_PublishVariantValue` | Feature flag variant retrieval |

> 💻 [`tests/TutorialLabs/Tutorial42/Lab.cs`](../tests/TutorialLabs/Tutorial42/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial42.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_MultiEnvironmentConfigDrivenRouting` | 🟢 Starter | Multi-environment config-driven routing |
| 2 | `Challenge2_FeatureFlagRolloutAndTenantTargeting` | 🟡 Intermediate | Feature flag rollout and tenant targeting |
| 3 | `Challenge3_ConfigChangeNotification_PublishToNatsBrokerEndpoint` | 🔴 Advanced | Config change notification via NatsBrokerEndpoint |

> 💻 [`tests/TutorialLabs/Tutorial42/Exam.cs`](../tests/TutorialLabs/Tutorial42/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial42.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial42.ExamAnswers"
```

---

**Previous: [← Tutorial 41 — OpenClaw Web UI](41-openclaw-web.md)** | **Next: [Tutorial 43 — Kubernetes Deployment →](43-kubernetes-deployment.md)**
