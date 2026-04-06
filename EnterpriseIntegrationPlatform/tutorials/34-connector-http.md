# Tutorial 34 — HTTP Connector

Deliver messages to external HTTP endpoints with retry and timeout configuration.

## Key Types

```csharp
// src/Connector.Http/IHttpConnector.cs
public interface IHttpConnector
{
    Task<TResponse> SendAsync<TPayload, TResponse>(
        IntegrationEnvelope<TPayload> envelope,
        string relativeUrl,
        HttpMethod method,
        CancellationToken ct);

    Task<TResponse> SendWithTokenAsync<TPayload, TResponse>(
        IntegrationEnvelope<TPayload> envelope,
        string relativeUrl,
        HttpMethod method,
        string tokenEndpoint,
        string tokenRequestBody,
        string tokenHeaderName,
        CancellationToken ct);
}
```

```csharp
// src/Connector.Http/HttpConnectorOptions.cs
public sealed class HttpConnectorOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public int CacheTokenExpirySeconds { get; set; } = 300;
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
}
```

```csharp
// src/Connectors/ConnectorResult.cs
public sealed record ConnectorResult
{
    public required bool Success { get; init; }
    public required string ConnectorName { get; init; }
    public string? StatusMessage { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;

    public static ConnectorResult Ok(string connectorName, string? statusMessage = null) =>
        new() { Success = true, ConnectorName = connectorName, StatusMessage = statusMessage };

    public static ConnectorResult Fail(string connectorName, string errorMessage) =>
        new() { Success = false, ConnectorName = connectorName, ErrorMessage = errorMessage };
}
```

## Exercises

### 1. TokenCache — SetAndGet Roundtrip

```csharp
var cache = new InMemoryTokenCache();

cache.SetToken("auth", "bearer-token-123", TimeSpan.FromMinutes(5));

var found = cache.TryGetToken("auth", out var token);

Assert.That(found, Is.True);
Assert.That(token, Is.EqualTo("bearer-token-123"));
```

### 2. TokenCache — MissingKey ReturnsFalse

```csharp
var cache = new InMemoryTokenCache();

var found = cache.TryGetToken("nonexistent", out var token);

Assert.That(found, Is.False);
Assert.That(token, Is.Null);
```

### 3. TokenCache — ExpiredToken ReturnsFalse

```csharp
var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
var cache = new InMemoryTokenCache(fakeTime);

cache.SetToken("auth", "token-value", TimeSpan.FromMinutes(1));

// Advance time past expiry
fakeTime.Advance(TimeSpan.FromMinutes(2));

var found = cache.TryGetToken("auth", out var token);

Assert.That(found, Is.False);
Assert.That(token, Is.Null);
```

### 4. HttpConnectorOptions — Defaults

```csharp
var opts = new HttpConnectorOptions();

Assert.That(opts.BaseUrl, Is.EqualTo(string.Empty));
Assert.That(opts.TimeoutSeconds, Is.EqualTo(30));
Assert.That(opts.MaxRetryAttempts, Is.EqualTo(3));
Assert.That(opts.RetryDelayMs, Is.EqualTo(1000));
Assert.That(opts.CacheTokenExpirySeconds, Is.EqualTo(300));
Assert.That(opts.DefaultHeaders, Is.Not.Null);
Assert.That(opts.DefaultHeaders, Is.Empty);
```

### 5. HttpConnectorOptions — CustomValues

```csharp
var opts = new HttpConnectorOptions
{
    BaseUrl = "https://api.example.com",
    TimeoutSeconds = 60,
    MaxRetryAttempts = 5,
    RetryDelayMs = 2000,
    CacheTokenExpirySeconds = 600,
    DefaultHeaders = new Dictionary<string, string>
    {
        ["X-Api-Key"] = "key123",
    },
};

Assert.That(opts.BaseUrl, Is.EqualTo("https://api.example.com"));
Assert.That(opts.TimeoutSeconds, Is.EqualTo(60));
Assert.That(opts.MaxRetryAttempts, Is.EqualTo(5));
Assert.That(opts.RetryDelayMs, Is.EqualTo(2000));
Assert.That(opts.CacheTokenExpirySeconds, Is.EqualTo(600));
Assert.That(opts.DefaultHeaders["X-Api-Key"], Is.EqualTo("key123"));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial34/Lab.cs`](../tests/TutorialLabs/Tutorial34/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial34.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial34/Exam.cs`](../tests/TutorialLabs/Tutorial34/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial34.Exam"
```

---

**Previous: [← Tutorial 33 — Security](33-security.md)** | **Next: [Tutorial 35 — SFTP Connector →](35-connector-sftp.md)**
