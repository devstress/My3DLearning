# Tutorial 34 — HTTP Connector

Deliver messages to external HTTP endpoints with retry and timeout configuration.

## Learning Objectives

After completing this tutorial you will be able to:

1. Configure an `HttpConnectorAdapter` with default and custom options
2. Send messages via HTTP and inspect success/failure `ConnectorResult`
3. Use `TokenCache` to store and expire bearer tokens
4. Understand the `IConnector` adapter pattern and its `Name`/`Type` properties
5. Publish HTTP connector results through a `MockEndpoint`

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

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Adapter_NameAndType_AreCorrect` | Adapter identity properties |
| 2 | `SendAsync_Success_ReturnsOkResult` | Successful send returns OK result |
| 3 | `SendAsync_Failure_ReturnsFailResult` | Failed send returns failure result |
| 4 | `SendAsync_DefaultDestination_UsesSlash` | Default destination falls back to `/` |
| 5 | `TokenCache_SetAndRetrieve` | Token cache set and retrieve |
| 6 | `TokenCache_Expired_ReturnsFalse` | Expired token returns false |
| 7 | `HttpConnectorOptions_Defaults` | Options default values |

> 💻 [`tests/TutorialLabs/Tutorial34/Lab.cs`](../tests/TutorialLabs/Tutorial34/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial34.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_SendToCustomDestination_PublishesResult` | 🟢 Starter | Send to custom destination and publish result |
| 2 | `Challenge2_TokenCachingLifecycle` | 🟡 Intermediate | Token caching lifecycle (set → hit → expire) |
| 3 | `Challenge3_MultipleConnectors_IndependentResults` | 🔴 Advanced | Multiple independent connectors with separate results |

> 💻 [`tests/TutorialLabs/Tutorial34/Exam.cs`](../tests/TutorialLabs/Tutorial34/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial34.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial34.ExamAnswers"
```

---

**Previous: [← Tutorial 33 — Security](33-security.md)** | **Next: [Tutorial 35 — SFTP Connector →](35-connector-sftp.md)**
