# Tutorial 42 — Configuration

Manage platform configuration with environment overrides, feature flags, and hot reload.

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

## Exercises

### 1. ConfigurationEntry — Defaults EnvironmentAndVersion

```csharp
var entry = new ConfigurationEntry("Database:Host", "localhost");

Assert.That(entry.Key, Is.EqualTo("Database:Host"));
Assert.That(entry.Value, Is.EqualTo("localhost"));
Assert.That(entry.Environment, Is.EqualTo("default"));
Assert.That(entry.Version, Is.EqualTo(1));
Assert.That(entry.ModifiedBy, Is.Null);
Assert.That(entry.LastModified, Is.Not.EqualTo(default(DateTimeOffset)));
```

### 2. InMemoryConfigurationStore — SetAndGet Roundtrip

```csharp
using var notifier = new ConfigurationChangeNotifier();
var store = new InMemoryConfigurationStore(notifier);

var entry = new ConfigurationEntry("App:Name", "MyApp");
await store.SetAsync(entry);

var retrieved = await store.GetAsync("App:Name");

Assert.That(retrieved, Is.Not.Null);
Assert.That(retrieved!.Value, Is.EqualTo("MyApp"));
Assert.That(retrieved.Version, Is.EqualTo(1));
```

### 3. InMemoryConfigurationStore — SetDeleteGet ReturnsNull

```csharp
using var notifier = new ConfigurationChangeNotifier();
var store = new InMemoryConfigurationStore(notifier);

await store.SetAsync(new ConfigurationEntry("Temp:Key", "value"));
var deleted = await store.DeleteAsync("Temp:Key");
var retrieved = await store.GetAsync("Temp:Key");

Assert.That(deleted, Is.True);
Assert.That(retrieved, Is.Null);
```

### 4. InMemoryConfigurationStore — List ReturnsAllEntries

```csharp
using var notifier = new ConfigurationChangeNotifier();
var store = new InMemoryConfigurationStore(notifier);

await store.SetAsync(new ConfigurationEntry("Key1", "Val1", "dev"));
await store.SetAsync(new ConfigurationEntry("Key2", "Val2", "dev"));
await store.SetAsync(new ConfigurationEntry("Key3", "Val3", "prod"));

var allEntries = await store.ListAsync();
Assert.That(allEntries, Has.Count.EqualTo(3));

var devEntries = await store.ListAsync("dev");
Assert.That(devEntries, Has.Count.EqualTo(2));
```

### 5. FeatureFlag — RecordShape

```csharp
var flag = new FeatureFlag(
    Name: "NewCheckout",
    IsEnabled: true,
    Variants: new Dictionary<string, string> { ["control"] = "v1", ["treatment"] = "v2" },
    RolloutPercentage: 50,
    TargetTenants: new List<string> { "tenant-a", "tenant-b" });

Assert.That(flag.Name, Is.EqualTo("NewCheckout"));
Assert.That(flag.IsEnabled, Is.True);
Assert.That(flag.Variants, Has.Count.EqualTo(2));
Assert.That(flag.RolloutPercentage, Is.EqualTo(50));
Assert.That(flag.TargetTenants, Has.Count.EqualTo(2));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial42/Lab.cs`](../tests/TutorialLabs/Tutorial42/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial42.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial42/Exam.cs`](../tests/TutorialLabs/Tutorial42/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial42.Exam"
```

---

**Previous: [← Tutorial 41 — OpenClaw Web UI](41-openclaw-web.md)** | **Next: [Tutorial 43 — Kubernetes Deployment →](43-kubernetes-deployment.md)**
