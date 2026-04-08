# Tutorial 37 — File Connector

Write messages to local or network file system paths with naming conventions.

## Learning Objectives

After completing this tutorial you will be able to:

1. Write data files with accompanying metadata sidecar files
2. Expand filename patterns using envelope properties (MessageType, MessageId)
3. Create directories on demand when the connector option is enabled
4. Handle file-exists conflicts based on overwrite settings
5. Read file content and list matching files from the file system
6. Wire an envelope through a `MockEndpoint` into the file connector end-to-end

## Key Types

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

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `Write_CreatesDataFile_AndMetadataSidecar` | Write creates data + metadata sidecar |
| 2 | `Write_ExpandsFilenamePattern_FromEnvelope` | Filename pattern expanded from envelope |
| 3 | `Write_CreatesDirectory_WhenOptionEnabled` | Directory auto-created when enabled |
| 4 | `Write_Throws_WhenFileExists_AndOverwriteDisabled` | Throws on existing file when overwrite disabled |
| 5 | `Read_ReturnsFileContent` | Read returns file content |
| 6 | `ListFiles_ReturnsMatchingPaths` | List files returns matching paths |
| 7 | `E2E_MockEndpoint_FeedsEnvelope_ThroughFileConnector` | End-to-end envelope → file connector |

> 💻 [`tests/TutorialLabs/Tutorial37/Lab.cs`](../tests/TutorialLabs/Tutorial37/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial37.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_WriteAndReadRoundtrip_ThroughMockEndpoint` | 🟢 Starter | Write-and-read round-trip through MockEndpoint |
| 2 | `Challenge2_CustomFilenamePattern_ContainsMessageTypeAndId` | 🟡 Intermediate | Custom filename pattern with MessageType and Id |
| 3 | `Challenge3_SubdirectoryListing_CombinesRootAndSub` | 🔴 Advanced | Subdirectory listing combines root and sub |

> 💻 [`tests/TutorialLabs/Tutorial37/Exam.cs`](../tests/TutorialLabs/Tutorial37/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial37.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial37.ExamAnswers"
```

---

**Previous: [← Tutorial 36 — Email Connector](36-connector-email.md)** | **Next: [Tutorial 38 — OpenTelemetry →](38-opentelemetry.md)**
