# Tutorial 37 — File Connector

## What You'll Learn

- The EIP Channel Adapter pattern for local and network file system integration
- How `IFileConnector` provides generic `WriteAsync<T>`, `ReadAsync`, and `ListFilesAsync` operations
- Configurable encoding for multi-language payload support
- Pattern matching for selective file discovery
- Directory auto-creation and overwrite control

---

## EIP Pattern: Channel Adapter (File)

> *"A File Channel Adapter bridges the messaging system and a file system, reading messages from files or writing messages to files."*
> — Gregor Hohpe & Bobby Woolf, *Enterprise Integration Patterns*

```
  Write path:
  ┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
  │  Pipeline    │────▶│  File Connector   │────▶│  File System │
  │  (envelope)  │     │  (write)          │     │  (local/NFS) │
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
// src/Connector.FileSystem/IFileConnector.cs
public interface IFileConnector
{
    Task<string> WriteAsync<T>(
        IntegrationEnvelope<T> envelope,
        Func<T, byte[]> serializer,
        CancellationToken ct);

    Task<byte[]> ReadAsync(string filePath, CancellationToken ct);

    Task<IReadOnlyList<string>> ListFilesAsync(
        string? subdirectory,
        string searchPattern,
        CancellationToken ct);
}
```

`WriteAsync<T>` accepts a generic envelope and a `Func<T, byte[]>` serializer, returning the full path of the written file. `ReadAsync` returns raw bytes. `ListFilesAsync` searches the `RootDirectory` (or an optional subdirectory) for files matching the given pattern.

### FileConnectorOptions

```csharp
// src/Connector.FileSystem/FileConnectorOptions.cs
public sealed class FileConnectorOptions
{
    public string RootDirectory { get; set; } = string.Empty;
    public string Encoding { get; set; } = "utf-8";
    public bool CreateDirectoryIfNotExists { get; set; } = true;
    public bool OverwriteExisting { get; set; } = false;
    public string FilenamePattern { get; set; } = "{MessageId}-{MessageType}.json";
}
```

| Option | Purpose |
|--------|---------|
| `RootDirectory` | Base directory for all file operations |
| `Encoding` | Character encoding name for text files (default `utf-8`) |
| `CreateDirectoryIfNotExists` | Automatically create the directory tree if it does not exist (default `true`) |
| `OverwriteExisting` | When `false`, throws if a file with the same name exists (default `false`) |
| `FilenamePattern` | Template for generated filenames with `{MessageId}`, `{MessageType}`, `{CorrelationId}`, `{Timestamp}` tokens |

`ListFilesAsync` supports standard search patterns (`*.json`, `order_*.xml`, `2024-01-*`) for selective file discovery.

---

## Scalability Dimension

File I/O is bound by disk throughput and network filesystem latency (for NFS/SMB mounts). The connector supports concurrent writes from multiple replicas because `{MessageId}` in the `FilenamePattern` ensures uniqueness. For read-side polling, use a single consumer to prevent duplicate ingestion, or implement a distributed lock if multiple readers are needed.

---

## Atomicity Dimension

`WriteAsync` ensures **all-or-nothing file delivery**. If `OverwriteExisting` is `false` and the target file exists, an `InvalidOperationException` is thrown and the source message is redelivered. The source message is Acked only after the write succeeds and the full path is returned. `CreateDirectoryIfNotExists` prevents failures due to missing directory trees.

---

## Exercises

1. Design a `FileConnectorOptions` for a batch process that writes UTF-16 encoded XML files to an NFS share, with `OverwriteExisting = true`.

2. Two consumer replicas write to the same `RootDirectory`. How does the `FilenamePattern` with `{MessageId}` prevent conflicts?

3. Why does the connector use `Func<T, byte[]>` for serialization rather than accepting raw strings?

---

**Previous: [← Tutorial 36 — Email Connector](36-connector-email.md)** | **Next: [Tutorial 38 — OpenTelemetry →](38-opentelemetry.md)**
