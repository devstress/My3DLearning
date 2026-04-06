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

## Lab

**Objective:** Design connection pooling for SFTP under high consumer concurrency, trace the upload lifecycle, and analyze **atomic** file delivery guarantees.

### Step 1: Design Connection Pooling

An SFTP server has a 10-connection limit. You have 20 consumer replicas uploading files:

| Without Pooling | With Pooling |
|-----------------|-------------|
| 20 connections attempted | Pool of 10 shared connections |
| 10 fail with connection refused | Consumers wait for available connection |
| No retry coordination | Queue + semaphore manages access |

Open `src/Connector.Sftp/SftpConnector.cs` and check: How does the platform pool SFTP connections? What happens when all pool slots are busy?

### Step 2: Trace the Upload Lifecycle

`UploadAsync<T>` accepts a `Func<T, byte[]>` serializer. Write a serializer for a JSON payload:

```csharp
var remotePath = await sftpConnector.UploadAsync(
    envelope,
    fileName: "order-42.json",
    serializer: payload => System.Text.Encoding.UTF8.GetBytes(
        System.Text.Json.JsonSerializer.Serialize(payload)),
    cancellationToken: ct);
```

Why does the connector return the full remote path rather than a `ConnectorResult`? How does the caller confirm **atomic** delivery — is the file visible to the receiver immediately or after a rename?

### Step 3: Design Atomic File Delivery

SFTP uploads are not atomic — a partial file can be read by the receiver mid-upload. Design a safe strategy:

```
1. Upload to temporary path: /incoming/orders/.tmp-{guid}
2. Rename to final path: /incoming/orders/order-42.json
3. Return the final path in ConnectorResult
```

If the upload fails after 50%, the temp file is cleaned up. If the rename fails, the temp file exists but is invisible to the receiver. How does this pattern guarantee **atomic** file visibility?

## Exam

1. Why is connection pooling essential for SFTP connector **scalability**?
   - A) SFTP servers have unlimited connections
   - B) SFTP servers have strict connection limits — without pooling, concurrent consumer replicas would exceed the limit and fail; pooling ensures connections are shared efficiently across all consumers
   - C) Pooling reduces file size
   - D) Each consumer needs its own dedicated SFTP server

2. How does the temp-file-then-rename pattern ensure **atomic** file delivery?
   - A) Renaming is faster than uploading
   - B) The receiver never sees partial files — the temp file is invisible to the receiver's file scanner, and the rename operation is atomic at the filesystem level, so the file transitions from invisible to complete in one step
   - C) The SFTP protocol guarantees atomicity
   - D) Temp files are automatically deleted after 30 seconds

3. What happens if the SFTP connection is lost during an upload?
   - A) The partial file is delivered to the receiver
   - B) The temp file remains on the server but is not renamed — the connector retries the entire upload; if all retries fail, the message is routed to the DLQ and the orphaned temp file can be cleaned up by a scheduled job
   - C) The connection automatically reconnects and resumes
   - D) The broker retries the upload internally

---

**Previous: [← Tutorial 34 — HTTP Connector](34-connector-http.md)** | **Next: [Tutorial 36 — Email Connector →](36-connector-email.md)**
