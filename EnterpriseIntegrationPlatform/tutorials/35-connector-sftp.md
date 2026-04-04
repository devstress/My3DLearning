# Tutorial 35 — SFTP Connector

## What You'll Learn

- The EIP Channel Adapter pattern applied to SFTP file transfer
- How `ISftpConnector` provides UploadAsync, DownloadAsync, and ListFilesAsync operations
- SSH key authentication for secure, passwordless connections
- Atomic rename to prevent partial file reads by downstream systems
- Metadata sidecar files that accompany each transferred file

---

## EIP Pattern: Channel Adapter (SFTP)

> *"A Channel Adapter connects the messaging system to a file-based integration endpoint. SFTP adapters translate integration messages into file operations on a remote server."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  ┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
  │  Pipeline    │────▶│  SFTP Connector   │────▶│  Remote SFTP │
  │  (envelope)  │     │  (upload/download)│     │  Server      │
  └──────────────┘     └──────────────────┘     └──────────────┘
                              │
                       ┌──────┴──────┐
                       │ SSH Key Auth│
                       └─────────────┘
```

Many enterprise integrations still rely on file-based exchange via SFTP. The connector bridges the messaging pipeline and the file system, handling authentication, atomic writes, and metadata tracking.

---

## Platform Implementation

### ISftpConnector

```csharp
// src/Connector.Sftp/ISftpConnector.cs
public interface ISftpConnector
{
    Task<ConnectorResult> UploadAsync(
        IntegrationEnvelope<string> envelope,
        SftpConnectorOptions options,
        CancellationToken cancellationToken = default);

    Task<IntegrationEnvelope<string>> DownloadAsync(
        string remotePath,
        SftpConnectorOptions options,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RemoteFileInfo>> ListFilesAsync(
        string remotePath,
        string? pattern,
        SftpConnectorOptions options,
        CancellationToken cancellationToken = default);
}
```

### SftpConnectorOptions

```csharp
// src/Connector.Sftp/SftpConnectorOptions.cs
public sealed class SftpConnectorOptions
{
    public required string Host { get; init; }
    public int Port { get; init; } = 22;
    public required string Username { get; init; }
    public string? Password { get; init; }
    public string? PrivateKeyPath { get; init; }
    public string? PrivateKeyPassphrase { get; init; }
    public required string RemoteDirectory { get; init; }
    public bool UseAtomicRename { get; init; } = true;
    public bool WriteSidecarMetadata { get; init; } = true;
    public string FileNameTemplate { get; init; } = "{MessageId}_{Timestamp}.dat";
    public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
```

### SSH Key Authentication

The connector supports two authentication modes:
- **Password** — `Username` + `Password` (simple but less secure)
- **SSH Key** — `Username` + `PrivateKeyPath` + optional `PrivateKeyPassphrase`

SSH key authentication is preferred for production. The private key is loaded from the path specified in options (retrieved via `ISecretProvider` from Tutorial 33 in secure deployments).

### Atomic Rename

When `UseAtomicRename = true`, the upload process:
1. Writes the file to `{RemoteDirectory}/{filename}.tmp`
2. Writes the metadata sidecar to `{RemoteDirectory}/{filename}.meta`
3. Renames `.tmp` → final filename

This ensures downstream consumers never read a partially written file. The rename operation is atomic on most SFTP server implementations.

### Metadata Sidecar Files

Each uploaded file is accompanied by a `.meta` JSON sidecar:

```json
{
  "messageId": "abc-123",
  "correlationId": "order-42",
  "source": "ERP-System",
  "uploadedAt": "2024-01-15T10:30:00Z",
  "originalContentType": "application/json",
  "tenantId": "tenant-a"
}
```

Sidecar files enable downstream file-based consumers to correlate files back to the integration pipeline without parsing the payload.

### RemoteFileInfo

```csharp
// src/Connector.Sftp/RemoteFileInfo.cs
public sealed record RemoteFileInfo(
    string FileName,
    string FullPath,
    long SizeBytes,
    DateTimeOffset LastModified);
```

---

## Scalability Dimension

SFTP connections are **expensive** — each connection requires a TCP handshake and SSH negotiation. The connector pools connections per host and reuses them across requests. Multiple consumer replicas can upload concurrently, but the remote server's connection limit must be respected. The `FileNameTemplate` uses `{MessageId}` to avoid filename collisions across replicas.

---

## Atomicity Dimension

The atomic rename strategy ensures **all-or-nothing delivery**. If the upload crashes before the rename, only a `.tmp` file exists — downstream consumers ignore it. If the rename succeeds, the file and its sidecar are both complete. The source message is Acked only after the rename succeeds. On failure, the connector cleans up the `.tmp` file and returns a failure `ConnectorResult`.

---

## Exercises

1. An SFTP server has a 10-connection limit. You have 20 consumer replicas uploading files. Design a connection pooling strategy.

2. A file upload succeeds but the metadata sidecar write fails. What should the connector do? How does atomic rename help?

3. Why does the platform use `.tmp` extension during upload rather than writing directly to the final filename?

---

**Previous: [← Tutorial 34 — HTTP Connector](34-connector-http.md)** | **Next: [Tutorial 36 — Email Connector →](36-connector-email.md)**
