# Tutorial 35 — SFTP Connector

Deliver messages as files to remote SFTP servers with connection pooling.

## Learning Objectives

After completing this tutorial you will be able to:

1. Configure `SftpConnectorOptions` with host, port, and credential defaults
2. Upload, download, and list files through the SFTP connector adapter
3. Verify that uploads create metadata sidecar files alongside data
4. Understand connection pool lifecycle with acquire and release
5. Publish upload results through a `MockEndpoint`

## Key Types

```csharp
// src/Connector.Sftp/ISftpConnector.cs
public interface ISftpConnector
{
    Task<string> UploadAsync<T>(
        IntegrationEnvelope<T> envelope,
        string fileName,
        Func<T, byte[]> serializer,
        CancellationToken ct);

    Task<byte[]> DownloadAsync(string remotePath, CancellationToken ct);

    Task<IReadOnlyList<string>> ListFilesAsync(string remotePath, CancellationToken ct);
}
```

```csharp
// src/Connector.Sftp/SftpConnectorOptions.cs
public sealed class SftpConnectorOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RootPath { get; set; } = "/";
    public int TimeoutMs { get; set; } = 10000;
}
```

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `SftpConnectorOptions_Defaults` | Default SFTP connector options |
| 2 | `Upload_DelegatesToPoolAndClient` | Upload delegates to pool and client |
| 3 | `Download_DelegatesToPoolAndClient` | Download delegates to pool and client |
| 4 | `ListFiles_DelegatesToPoolAndClient` | ListFiles delegates to pool and client |
| 5 | `Upload_CreatesMetadataSidecar` | Upload creates metadata sidecar file |
| 6 | `PoolRelease_CalledAfterUpload` | Pool connection released after upload |
| 7 | `UploadResult_PublishedToMockEndpoint` | Upload result published end-to-end |

> 💻 [`tests/TutorialLabs/Tutorial35/Lab.cs`](../tests/TutorialLabs/Tutorial35/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial35.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_ConnectionPoolLifecycle` | 🟢 Starter | Connection pool acquire → use → release lifecycle |
| 2 | `Challenge2_UploadSerializationRoundTrip` | 🟡 Intermediate | Upload serialisation round-trip |
| 3 | `Challenge3_AdapterImplementsIConnector` | 🔴 Advanced | Adapter implements IConnector interface |

> 💻 [`tests/TutorialLabs/Tutorial35/Exam.cs`](../tests/TutorialLabs/Tutorial35/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial35.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial35.ExamAnswers"
```

---

**Previous: [← Tutorial 34 — HTTP Connector](34-connector-http.md)** | **Next: [Tutorial 36 — Email Connector →](36-connector-email.md)**
