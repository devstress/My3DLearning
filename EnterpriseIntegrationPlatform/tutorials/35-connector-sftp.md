# Tutorial 35 — SFTP Connector

## What You'll Learn

- The EIP Channel Adapter pattern applied to SFTP file transfer
- How `ISftpConnector` provides generic `UploadAsync<T>`, `DownloadAsync`, and `ListFilesAsync` operations
- Password authentication for SFTP connections
- Configurable root path and timeout settings

---

## EIP Pattern: Channel Adapter (SFTP)

> *"A Channel Adapter connects the messaging system to a file-based integration endpoint. SFTP adapters translate integration messages into file operations on a remote server."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  ┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
  │  Pipeline    │────▶│  SFTP Connector   │────▶│  Remote SFTP │
  │  (envelope)  │     │  (upload/download)│     │  Server      │
  └──────────────┘     └──────────────────┘     └──────────────┘
```

Many enterprise integrations still rely on file-based exchange via SFTP. The connector bridges the messaging pipeline and the file system, handling authentication and file transfer.

---

## Platform Implementation

### ISftpConnector

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

`UploadAsync<T>` accepts a generic envelope and a `Func<T, byte[]>` serializer that converts the payload to bytes. It returns the full remote path of the uploaded file. `DownloadAsync` returns raw bytes, and `ListFilesAsync` returns a list of full remote file paths.

### SftpConnectorOptions

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

| Option | Purpose |
|--------|---------|
| `Host` | SFTP server hostname or IP address |
| `Port` | SFTP server port (default 22) |
| `Username` | SFTP authentication username |
| `Password` | SFTP authentication password |
| `RootPath` | Root path on the remote server (default `/`) |
| `TimeoutMs` | Connection timeout in milliseconds (default 10000) |

---

## Scalability Dimension

SFTP connections are **expensive** — each connection requires a TCP handshake and SSH negotiation. The connector pools connections per host and reuses them across requests. Multiple consumer replicas can upload concurrently, but the remote server's connection limit must be respected. Using unique filenames (e.g. based on `MessageId`) avoids filename collisions across replicas.

---

## Atomicity Dimension

The `UploadAsync` method ensures **all-or-nothing delivery**. If the upload fails, no file is left on the server and the source message is redelivered. The source message is Acked only after the upload succeeds and the full remote path is returned. On failure, the connector returns an error and the message is retried or dead-lettered.

---

## Exercises

1. An SFTP server has a 10-connection limit. You have 20 consumer replicas uploading files. Design a connection pooling strategy.

2. `UploadAsync<T>` accepts a `Func<T, byte[]>` serializer. Write a serializer lambda for a JSON payload of type `OrderPayload`.

3. Why does the connector return the full remote path from `UploadAsync` rather than a `ConnectorResult`?

---

**Previous: [← Tutorial 34 — HTTP Connector](34-connector-http.md)** | **Next: [Tutorial 36 — Email Connector →](36-connector-email.md)**
