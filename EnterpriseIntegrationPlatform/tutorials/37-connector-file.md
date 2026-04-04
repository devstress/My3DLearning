# Tutorial 37 — File Connector

## What You'll Learn

- The EIP Channel Adapter pattern for local and network file system integration
- How `IFileConnector` provides WriteAsync, ReadAsync, and ListFilesAsync operations
- Atomic write using temporary files and rename
- Configurable encoding for multi-language payload support
- Pattern matching for selective file discovery
- Metadata sidecar files for traceability

---

## EIP Pattern: Channel Adapter (File)

> *"A File Channel Adapter bridges the messaging system and a file system, reading messages from files or writing messages to files."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  Write path:
  ┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
  │  Pipeline    │────▶│  File Connector   │────▶│  File System │
  │  (envelope)  │     │  (atomic write)   │     │  (local/NFS) │
  └──────────────┘     └──────────────────┘     └──────────────┘

  Read path:
  ┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
  │  File System │────▶│  File Connector   │────▶│  Pipeline    │
  │  (polled)    │     │  (read + ingest)  │     │  (envelope)  │
  └──────────────┘     └──────────────────┘     └──────────────┘
```

File-based integration is common in batch processing, legacy system interop, and regulated industries that require file-based audit trails.

---

## Platform Implementation

### IFileConnector

```csharp
// src/Connector.File/IFileConnector.cs
public interface IFileConnector
{
    Task<ConnectorResult> WriteAsync(
        IntegrationEnvelope<string> envelope,
        FileConnectorOptions options,
        CancellationToken cancellationToken = default);

    Task<IntegrationEnvelope<string>> ReadAsync(
        string filePath,
        FileConnectorOptions options,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalFileInfo>> ListFilesAsync(
        string directory,
        string? pattern,
        CancellationToken cancellationToken = default);
}
```

### FileConnectorOptions

```csharp
// src/Connector.File/FileConnectorOptions.cs
public sealed class FileConnectorOptions
{
    public required string Directory { get; init; }
    public string FileNameTemplate { get; init; } = "{MessageId}_{Timestamp}.json";
    public Encoding Encoding { get; init; } = Encoding.UTF8;
    public bool UseAtomicWrite { get; init; } = true;
    public bool WriteSidecarMetadata { get; init; } = true;
    public string? FilePattern { get; init; }         // e.g. "*.json", "order_*.xml"
    public bool DeleteAfterRead { get; init; } = false;
}
```

| Option | Purpose |
|--------|---------|
| `FileNameTemplate` | Template for generated filenames with `{MessageId}`, `{Timestamp}`, `{Source}` placeholders |
| `Encoding` | Character encoding (UTF-8, UTF-16, ASCII, ISO-8859-1) for multi-language support |
| `UseAtomicWrite` | Write to `.tmp` then rename — prevents partial reads |
| `WriteSidecarMetadata` | Generate `.meta` JSON file alongside the data file |
| `FilePattern` | Glob pattern for `ListFilesAsync` filtering |
| `DeleteAfterRead` | Remove file after successful read and ingestion |

### Atomic Write

When `UseAtomicWrite = true`, the write process:
1. Generates the target filename from the template
2. Writes content to `{filename}.tmp` with the configured encoding
3. Writes the sidecar `.meta` file (if enabled)
4. Renames `.tmp` → final filename

This guarantees that any process polling the directory sees only complete files.

### Metadata Sidecar

```json
{
  "messageId": "msg-456",
  "correlationId": "batch-789",
  "source": "InventorySystem",
  "writtenAt": "2024-01-15T14:00:00Z",
  "encoding": "utf-8",
  "originalContentType": "application/json",
  "tenantId": "tenant-b"
}
```

### LocalFileInfo

```csharp
// src/Connector.File/LocalFileInfo.cs
public sealed record LocalFileInfo(
    string FileName,
    string FullPath,
    long SizeBytes,
    DateTimeOffset LastModified,
    bool HasSidecar);
```

`ListFilesAsync` supports standard glob patterns (`*.json`, `order_*.xml`, `2024-01-*`) for selective file discovery.

---

## Scalability Dimension

File I/O is bound by disk throughput and network filesystem latency (for NFS/SMB mounts). The connector supports concurrent writes from multiple replicas because `{MessageId}` in the filename template ensures uniqueness. For read-side polling, use a single consumer with `DeleteAfterRead = true` to prevent duplicate ingestion, or implement a distributed lock if multiple readers are needed.

---

## Atomicity Dimension

Atomic write ensures **all-or-nothing file delivery**. If the process crashes during the `.tmp` write phase, no final file exists and the source message is redelivered. The rename from `.tmp` to the final name is atomic on local filesystems and most NFS implementations. The source message is Acked only after the rename succeeds. When `DeleteAfterRead = true`, the file is deleted only after the message is successfully published to the pipeline.

---

## Exercises

1. Design a `FileConnectorOptions` for a batch process that writes UTF-16 encoded XML files with sidecar metadata to an NFS share.

2. Two consumer replicas poll the same directory with `DeleteAfterRead = true`. What race condition can occur? How would you prevent it?

3. Why does the connector use a separate `.meta` sidecar file instead of embedding metadata in the data file itself?

---

**Previous: [← Tutorial 36 — Email Connector](36-connector-email.md)** | **Next: [Tutorial 38 — OpenTelemetry →](38-opentelemetry.md)**
