# Tutorial 37 — File Connector

Write messages to local or network file system paths with naming conventions.

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

## Exercises

### 1. FileConnectorOptions — Defaults

```csharp
var opts = new FileConnectorOptions();

Assert.That(opts.RootDirectory, Is.EqualTo(string.Empty));
Assert.That(opts.Encoding, Is.EqualTo("utf-8"));
Assert.That(opts.CreateDirectoryIfNotExists, Is.True);
Assert.That(opts.OverwriteExisting, Is.False);
Assert.That(opts.FilenamePattern, Is.EqualTo("{MessageId}-{MessageType}.json"));
```

### 2. FileConnectorOptions — CustomValues

```csharp
var opts = new FileConnectorOptions
{
    RootDirectory = "/data/exports",
    Encoding = "ascii",
    CreateDirectoryIfNotExists = false,
    OverwriteExisting = true,
    FilenamePattern = "{CorrelationId}.xml",
};

Assert.That(opts.RootDirectory, Is.EqualTo("/data/exports"));
Assert.That(opts.Encoding, Is.EqualTo("ascii"));
Assert.That(opts.CreateDirectoryIfNotExists, Is.False);
Assert.That(opts.OverwriteExisting, Is.True);
Assert.That(opts.FilenamePattern, Is.EqualTo("{CorrelationId}.xml"));
```

### 3. IFileSystem — InterfaceShape HasExpectedMembers

```csharp
var type = typeof(IFileSystem);

Assert.That(type.GetMethod("WriteAllBytesAsync"), Is.Not.Null);
Assert.That(type.GetMethod("ReadAllBytesAsync"), Is.Not.Null);
Assert.That(type.GetMethod("GetFiles"), Is.Not.Null);
Assert.That(type.GetMethod("FileExists"), Is.Not.Null);
Assert.That(type.GetMethod("CreateDirectory"), Is.Not.Null);
```

### 4. FileConnector — Write DelegatesToFileSystem

```csharp
var fs = Substitute.For<IFileSystem>();

var opts = Options.Create(new FileConnectorOptions
{
    RootDirectory = "/output",
    CreateDirectoryIfNotExists = true,
});

var connector = new FileConnector(fs, opts, NullLogger<FileConnector>.Instance);

var envelope = IntegrationEnvelope<string>.Create("payload", "Svc", "order.placed");

await connector.WriteAsync(
    envelope,
    s => System.Text.Encoding.UTF8.GetBytes(s),
    CancellationToken.None);

// Verify directory creation was called
fs.Received(1).CreateDirectory(Arg.Any<string>());

// Verify file write was called (data + metadata sidecar)
await fs.Received(2).WriteAllBytesAsync(
    Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
```

### 5. FileConnector — Read DelegatesToFileSystem

```csharp
var fs = Substitute.For<IFileSystem>();
var expected = System.Text.Encoding.UTF8.GetBytes("file-content");
fs.ReadAllBytesAsync("/output/test.json", Arg.Any<CancellationToken>())
    .Returns(expected);

var connector = new FileConnector(
    fs,
    Options.Create(new FileConnectorOptions { RootDirectory = "/output" }),
    NullLogger<FileConnector>.Instance);

var result = await connector.ReadAsync("/output/test.json", CancellationToken.None);

Assert.That(result, Is.EqualTo(expected));
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial37/Lab.cs`](../tests/TutorialLabs/Tutorial37/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial37.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial37/Exam.cs`](../tests/TutorialLabs/Tutorial37/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial37.Exam"
```

---

**Previous: [← Tutorial 36 — Email Connector](36-connector-email.md)** | **Next: [Tutorial 38 — OpenTelemetry →](38-opentelemetry.md)**
