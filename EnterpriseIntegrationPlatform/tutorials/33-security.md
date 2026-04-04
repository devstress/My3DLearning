# Tutorial 33 — Security

## What You'll Learn

- How `IInputSanitizer` strips dangerous content from message payloads
- `IPayloadSizeGuard` enforces maximum payload size limits
- Script tag removal, SQL injection pattern detection, and HTML entity sanitization
- `PayloadTooLargeException` for oversized payloads
- `JwtOptions` for configuring JWT authentication and validation
- `Security.Secrets` for integrating with Azure Key Vault and HashiCorp Vault

---

## EIP Pattern: Message Filter (Security)

> *"A security-focused Message Filter removes or rejects messages that contain dangerous content before they reach the processing pipeline."*

```
  ┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
  │  Ingress     │────▶│  Input Sanitizer  │────▶│  Payload     │
  │  (raw)       │     │  (strip scripts,  │     │  Size Guard  │
  └──────────────┘     │   SQL, HTML)      │     └──────┬───────┘
                       └──────────────────┘            │
                                                       ▼
                                              ┌──────────────────┐
                                              │  Pipeline        │
                                              │  (safe payload)  │
                                              └──────────────────┘
```

The sanitizer and size guard act as a security gateway. They run before any business logic, ensuring that only clean, appropriately-sized payloads enter the pipeline.

---

## Platform Implementation

### IInputSanitizer

```csharp
// src/Security/IInputSanitizer.cs
public interface IInputSanitizer
{
    Task<SanitizationResult> SanitizeAsync(
        string payload,
        CancellationToken cancellationToken = default);
}

public sealed record SanitizationResult(
    string SanitizedPayload,
    bool WasModified,
    IReadOnlyList<string> RemovedPatterns);
```

The sanitizer detects and removes:
- **Script tags**: `<script>...</script>`, inline event handlers (`onclick`, `onerror`)
- **SQL injection patterns**: `'; DROP TABLE`, `OR 1=1`, `UNION SELECT`, comment sequences
- **HTML entities**: encoded characters used to bypass text-based filters (`&#60;`, `&lt;`)
- **Control characters**: null bytes, Unicode direction overrides

The `RemovedPatterns` list provides an audit trail of what was stripped, useful for forensic analysis.

### IPayloadSizeGuard

```csharp
// src/Security/IPayloadSizeGuard.cs
public interface IPayloadSizeGuard
{
    void Validate(IntegrationEnvelope<string> envelope);
}
```

### PayloadTooLargeException

```csharp
// src/Security/PayloadTooLargeException.cs
public sealed class PayloadTooLargeException : Exception
{
    public long ActualSize { get; }
    public long MaxAllowedSize { get; }
}
```

When the payload exceeds the configured limit, the guard throws `PayloadTooLargeException`. This is a non-retryable error — the message is dead-lettered with `DeadLetterReason.ValidationFailed`.

### JwtOptions

```csharp
// src/Security/JwtOptions.cs
public sealed class JwtOptions
{
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string SigningKey { get; init; }
    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromHours(1);
    public bool ValidateLifetime { get; init; } = true;
}
```

JWT authentication is used at the Gateway API layer. Tokens carry tenant identity (Tutorial 32) and scopes. The `JwtOptions` configure validation parameters.

### Security.Secrets

```csharp
// src/Security.Secrets/ISecretProvider.cs
public interface ISecretProvider
{
    Task<string> GetSecretAsync(string secretName, CancellationToken ct);
    Task SetSecretAsync(string secretName, string value, CancellationToken ct);
}
```

Two implementations are provided:
- `AzureKeyVaultSecretProvider` — integrates with Azure Key Vault using managed identity
- `HashiCorpVaultSecretProvider` — integrates with HashiCorp Vault using AppRole or token auth

Secrets are never stored in configuration files or environment variables in production. The `ISecretProvider` abstraction allows swapping providers without code changes.

---

## Scalability Dimension

The sanitizer and size guard are **stateless and CPU-bound** — they inspect each payload independently. They run as early pipeline stages, rejecting bad messages before expensive processing occurs. This protects downstream systems from wasted work. In high-throughput deployments, sanitization can be the bottleneck; profiling guides replica count.

---

## Atomicity Dimension

Sanitization runs **before the message is Acked**. If the sanitizer modifies a payload, the `WasModified` flag ensures downstream components know the payload was altered. If the size guard rejects a message, it is Nacked and dead-lettered in a single atomic operation — the original message is never lost, just quarantined for inspection.

---

## Exercises

1. A payload contains `<script>alert('xss')</script>` embedded in a JSON string value. Describe how the sanitizer detects and removes it while preserving valid JSON structure.

2. Why does the platform use a separate `IPayloadSizeGuard` instead of checking size inside `IInputSanitizer`?

3. Compare `AzureKeyVaultSecretProvider` and `HashiCorpVaultSecretProvider`. When would you choose one over the other?

---

**Previous: [← Tutorial 32 — Multi-Tenancy](32-multi-tenancy.md)** | **Next: [Tutorial 34 — HTTP Connector →](34-connector-http.md)**
