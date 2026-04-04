# Tutorial 32 — Multi-Tenancy

## What You'll Learn

- How `ITenantResolver` identifies the current tenant from message metadata or HTTP headers
- `ITenantIsolationGuard` enforces cross-tenant data boundaries
- `TenantContext` carries tenant identity through the pipeline
- `TenantIsolationException` when a message crosses tenant boundaries
- Anonymous tenant handling for unauthenticated or system messages
- `MultiTenancy.Onboarding` for self-service tenant provisioning

---

## EIP Pattern: Message Metadata (Tenant Header)

> *"Attach tenant identity as message metadata so every component in the pipeline can enforce data isolation without coupling to the resolution mechanism."*

```
  ┌──────────────┐     ┌──────────────────┐
  │  Ingress     │────▶│  Tenant Resolver  │
  │  (header or  │     │  (metadata /      │
  │   metadata)  │     │   header lookup)  │
  └──────────────┘     └───────┬──────────┘
                               │
                     TenantContext flows
                     through pipeline
                               │
                    ┌──────────▼──────────┐
                    │  Isolation Guard    │
                    │  (cross-tenant      │
                    │   boundary check)   │
                    └─────────────────────┘
```

Every message entering the platform passes through tenant resolution. The resolved `TenantContext` propagates through all pipeline stages, and the isolation guard rejects any operation that would cross tenant boundaries.

---

## Platform Implementation

### ITenantResolver

```csharp
// src/MultiTenancy/ITenantResolver.cs
public interface ITenantResolver
{
    TenantContext Resolve(IReadOnlyDictionary<string, string> metadata);
    TenantContext Resolve(string? tenantId);
}
```

Both `Resolve` overloads are synchronous. The metadata overload checks (in order): the `X-Tenant-Id` header, the `TenantId` metadata key, and the authenticated identity's tenant claim. The string overload resolves directly from a tenant ID. If no tenant is found, either overload returns the **anonymous tenant context** (`TenantContext.Anonymous`).

### TenantContext

```csharp
// src/MultiTenancy/TenantContext.cs
public sealed class TenantContext
{
    public required string TenantId { get; init; }
    public string? TenantName { get; init; }
    public bool IsResolved { get; init; }

    public static TenantContext Anonymous => new()
    {
        TenantId = "anonymous",
        IsResolved = false
    };
}
```

### ITenantIsolationGuard

```csharp
// src/MultiTenancy/ITenantIsolationGuard.cs
public interface ITenantIsolationGuard
{
    void Enforce<T>(IntegrationEnvelope<T> envelope, string expectedTenantId);
}
```

### TenantIsolationException

```csharp
// src/MultiTenancy/TenantIsolationException.cs
public sealed class TenantIsolationException : Exception
{
    public Guid MessageId { get; }
    public string? ActualTenantId { get; }
    public string ExpectedTenantId { get; }
}
```

When a message's actual tenant does not match the expected tenant, the guard throws `TenantIsolationException` with the `MessageId`, `ActualTenantId`, and `ExpectedTenantId`. This is **non-retryable** — the message goes directly to the DLQ.

### MultiTenancy.Onboarding

```csharp
// src/MultiTenancy.Onboarding/ITenantOnboardingService.cs
public interface ITenantOnboardingService
{
    Task<TenantContext> OnboardAsync(
        TenantOnboardingRequest request,
        CancellationToken ct);

    Task OffboardAsync(string tenantId, CancellationToken ct);
}

public sealed record TenantOnboardingRequest(
    string TenantName,
    string AdminEmail,
    IDictionary<string, string>? Properties = null);
```

Onboarding provisions: tenant-specific broker topics, throttle policies (Tutorial 29), configuration namespaces, and an admin user. Offboarding archives data and removes access.

---

## Scalability Dimension

Tenant isolation enables **horizontal scaling by tenant**. High-volume tenants can be assigned dedicated consumer groups and broker partitions while small tenants share resources. The `TenantContext.TenantId` and `TenantName` identify each tenant, while `IsResolved` distinguishes resolved tenants from anonymous ones. Per-tenant quotas and feature flags can be managed through external configuration, enabling per-tenant scaling decisions in the competing consumers orchestrator (Tutorial 28).

---

## Atomicity Dimension

The isolation guard runs **before any processing** — a cross-tenant message is rejected atomically before it can corrupt another tenant's data. The `TenantContext` is resolved once at ingress and propagated immutably through the pipeline. This ensures that every component sees the same tenant identity, preventing time-of-check/time-of-use races.

---

## Exercises

1. A message arrives with `X-Tenant-Id: tenant-a` but the JWT claim says `tenant-b`. How should the resolver handle this conflict?

2. Describe the self-service flow when a new tenant onboards: what resources are provisioned and in what order?

3. Why is `TenantIsolationException` non-retryable? Under what circumstances could a cross-tenant message be legitimate?

---

**Previous: [← Tutorial 31 — Event Sourcing](31-event-sourcing.md)** | **Next: [Tutorial 33 — Security →](33-security.md)**
