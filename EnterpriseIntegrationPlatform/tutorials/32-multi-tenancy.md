# Tutorial 32 — Multi-Tenancy

Isolate message processing per tenant with tenant resolution and context propagation.

## Key Types

```csharp
// src/MultiTenancy/ITenantResolver.cs
public interface ITenantResolver
{
    TenantContext Resolve(IReadOnlyDictionary<string, string> metadata);
    TenantContext Resolve(string? tenantId);
}
```

```csharp
// src/MultiTenancy/TenantContext.cs
public sealed class TenantContext
{
    public required string TenantId { get; init; }
    public string? TenantName { get; init; }
    public bool IsResolved { get; init; }

    public static readonly TenantContext Anonymous = new()
    {
        TenantId = "anonymous",
        IsResolved = false,
    };
}
```

```csharp
// src/MultiTenancy/ITenantIsolationGuard.cs
public interface ITenantIsolationGuard
{
    void Enforce<T>(IntegrationEnvelope<T> envelope, string expectedTenantId);
}
```

```csharp
// src/MultiTenancy/TenantIsolationException.cs
public sealed class TenantIsolationException : Exception
{
    public Guid MessageId { get; }
    public string? ActualTenantId { get; }
    public string ExpectedTenantId { get; }
}
```

## Exercises

### 1. Resolve — FromMetadata WithTenantIdKey

```csharp
var metadata = new Dictionary<string, string>
{
    [TenantResolver.TenantMetadataKey] = "tenant-abc",
};

var context = _resolver.Resolve(metadata);

Assert.That(context.TenantId, Is.EqualTo("tenant-abc"));
Assert.That(context.IsResolved, Is.True);
```

### 2. Resolve — MissingTenantId ReturnsAnonymous

```csharp
var metadata = new Dictionary<string, string>();

var context = _resolver.Resolve(metadata);

Assert.That(context.IsResolved, Is.False);
Assert.That(context, Is.SameAs(TenantContext.Anonymous));
```

### 3. Resolve — String WithExplicitTenantId

```csharp
var context = _resolver.Resolve("my-tenant");

Assert.That(context.TenantId, Is.EqualTo("my-tenant"));
Assert.That(context.IsResolved, Is.True);
```

### 4. IsolationGuard — Enforce PassesWhenTenantMatches

```csharp
var guard = new TenantIsolationGuard(_resolver);
var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "event") with
{
    Metadata = new Dictionary<string, string>
    {
        [TenantResolver.TenantMetadataKey] = "tenant-x",
    },
};

Assert.DoesNotThrow(() => guard.Enforce(envelope, "tenant-x"));
```

### 5. IsolationGuard — Enforce ThrowsOnMismatch

```csharp
var guard = new TenantIsolationGuard(_resolver);
var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "event") with
{
    Metadata = new Dictionary<string, string>
    {
        [TenantResolver.TenantMetadataKey] = "tenant-a",
    },
};

var ex = Assert.Throws<TenantIsolationException>(
    () => guard.Enforce(envelope, "tenant-b"));

Assert.That(ex!.ActualTenantId, Is.EqualTo("tenant-a"));
Assert.That(ex.ExpectedTenantId, Is.EqualTo("tenant-b"));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial32/Lab.cs`](../tests/TutorialLabs/Tutorial32/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial32.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial32/Exam.cs`](../tests/TutorialLabs/Tutorial32/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial32.Exam"
```

---

**Previous: [← Tutorial 31 — Event Sourcing](31-event-sourcing.md)** | **Next: [Tutorial 33 — Security →](33-security.md)**
