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
// src/Connector.File/IFileConnector.cs
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
// src/Connector.File/FileConnectorOptions.cs
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

## Lab

**Objective:** Configure file-based delivery for batch processing, analyze concurrent write safety, and trace how `{MessageId}` filenames prevent conflicts in **scaled** consumer deployments.

### Step 1: Configure a File Connector

Design a `FileConnectorOptions` for a batch process that writes UTF-16 encoded XML files:

```csharp
var options = new FileConnectorOptions
{
    RootDirectory = "/mnt/nfs/outbound/orders",
    FilenamePattern = "order-{MessageId}.xml",
    Encoding = "utf-16",
    OverwriteExisting = true,
    CreateDirectoryIfMissing = true
};
```

Open `src/Connectors.File/FileConnector.cs` and trace: How does the connector resolve `{MessageId}` in the filename? What other placeholders are available?

### Step 2: Analyze Concurrent Write Safety

Two consumer replicas write to the same `RootDirectory`. With `{MessageId}` in the pattern:

| Replica | Message | Filename |
|---------|---------|----------|
| A | `msg-abc-123` | `order-abc-123.xml` |
| B | `msg-def-456` | `order-def-456.xml` |
| A | `msg-abc-123` (redelivery) | `order-abc-123.xml` (overwrite) |

How does `{MessageId}` prevent filename collisions between different messages? How does `OverwriteExisting` handle idempotent redelivery of the same message?

### Step 3: Evaluate File Connector Atomicity

The file connector uses `Func<T, byte[]>` for serialization rather than accepting raw strings. Explain:

- Why `byte[]` instead of `string`? (hint: binary formats like Avro, Protocol Buffers)
- How does the connector ensure **atomic** file creation? (hint: write to temp file, then rename)
- What happens if the process crashes after writing but before renaming?

## Exam

1. Why does the file connector use `{MessageId}` in the filename pattern?
   - A) Message IDs are shorter than timestamps
   - B) `MessageId` is globally unique — using it in filenames prevents collision when multiple consumer replicas write to the same directory, and makes redeliveries idempotent (same file is overwritten, not duplicated)
   - C) The filesystem requires GUIDs as filenames
   - D) MessageId is the only available placeholder

2. Why does the connector use `Func<T, byte[]>` for serialization?
   - A) Bytes are smaller than strings
   - B) `byte[]` supports any output format — JSON, XML, binary (Avro, Protobuf) — while `string` would limit the connector to text-only formats; this makes the connector **format-agnostic** and scalable across integration needs
   - C) The filesystem only stores bytes
   - D) String serialization is not supported in .NET

3. How does write-then-rename ensure **atomic** file visibility?
   - A) Renaming is faster than writing
   - B) The receiver's file scanner only sees the final filename — the temp file is invisible to scanners looking for the expected pattern; rename is atomic at the OS level, so the file transitions from invisible to complete in one step
   - C) The filesystem guarantees transactional writes
   - D) Temp files are automatically deleted

---

**Previous: [← Tutorial 36 — Email Connector](36-connector-email.md)** | **Next: [Tutorial 38 — OpenTelemetry →](38-opentelemetry.md)**
