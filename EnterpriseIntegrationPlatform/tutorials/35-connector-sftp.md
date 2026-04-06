# Tutorial 35 — SFTP Connector

Deliver messages as files to remote SFTP servers with connection pooling.

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

## Exercises

### 1. SftpConnectorOptions — Defaults

```csharp
var opts = new SftpConnectorOptions();

Assert.That(opts.Host, Is.EqualTo(string.Empty));
Assert.That(opts.Port, Is.EqualTo(22));
Assert.That(opts.Username, Is.EqualTo(string.Empty));
Assert.That(opts.Password, Is.EqualTo(string.Empty));
Assert.That(opts.RootPath, Is.EqualTo("/"));
Assert.That(opts.TimeoutMs, Is.EqualTo(10000));
Assert.That(opts.MaxConnectionsPerHost, Is.EqualTo(5));
```

### 2. SftpConnectorOptions — CustomValues

```csharp
var opts = new SftpConnectorOptions
{
    Host = "sftp.example.com",
    Port = 2222,
    Username = "deploy",
    Password = "p@ss",
    RootPath = "/uploads",
    TimeoutMs = 5000,
    MaxConnectionsPerHost = 10,
};

Assert.That(opts.Host, Is.EqualTo("sftp.example.com"));
Assert.That(opts.Port, Is.EqualTo(2222));
Assert.That(opts.Username, Is.EqualTo("deploy"));
Assert.That(opts.Password, Is.EqualTo("p@ss"));
Assert.That(opts.RootPath, Is.EqualTo("/uploads"));
Assert.That(opts.TimeoutMs, Is.EqualTo(5000));
Assert.That(opts.MaxConnectionsPerHost, Is.EqualTo(10));
```

### 3. ISftpClient — InterfaceShape HasExpectedMethods

```csharp
var type = typeof(ISftpClient);

Assert.That(type.GetMethod("Connect"), Is.Not.Null);
Assert.That(type.GetMethod("Disconnect"), Is.Not.Null);
Assert.That(type.GetMethod("UploadFile"), Is.Not.Null);
Assert.That(type.GetMethod("DownloadFile"), Is.Not.Null);
Assert.That(type.GetMethod("ListFiles"), Is.Not.Null);
Assert.That(type.GetMethod("DeleteFile"), Is.Not.Null);
Assert.That(type.GetProperty("IsConnected"), Is.Not.Null);
```

### 4. SftpConnectionPool — AcquireAndRelease

```csharp
var mockClient = Substitute.For<ISftpClient>();
mockClient.IsConnected.Returns(true);

var pool = new SftpConnectionPool(
    () => mockClient,
    Options.Create(new SftpConnectorOptions { MaxConnectionsPerHost = 2 }),
    NullLogger<SftpConnectionPool>.Instance);

var client = await pool.AcquireAsync();
Assert.That(client, Is.Not.Null);

mockClient.Received(1).Connect();

pool.Release(client);

await pool.DisposeAsync();
```

### 5. SftpConnector — Upload DelegatesToPool

```csharp
var mockClient = Substitute.For<ISftpClient>();
mockClient.IsConnected.Returns(true);

var mockPool = Substitute.For<ISftpConnectionPool>();
mockPool.AcquireAsync(Arg.Any<CancellationToken>()).Returns(mockClient);

var connector = new SftpConnector(
    mockPool,
    Options.Create(new SftpConnectorOptions { RootPath = "/data" }),
    NullLogger<SftpConnector>.Instance);

var envelope = IntegrationEnvelope<string>.Create("payload", "Svc", "file.upload");

var remotePath = await connector.UploadAsync(
    envelope, "test.json", s => System.Text.Encoding.UTF8.GetBytes(s), CancellationToken.None);

Assert.That(remotePath, Is.EqualTo("/data/test.json"));
mockClient.Received(2).UploadFile(Arg.Any<Stream>(), Arg.Any<string>()); // data + meta
mockPool.Received(1).Release(mockClient);
```

## Lab

Run the full lab: [`tests/TutorialLabs/Tutorial35/Lab.cs`](../tests/TutorialLabs/Tutorial35/Lab.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial35.Lab"
```

## Exam

Coding challenges: [`tests/TutorialLabs/Tutorial35/Exam.cs`](../tests/TutorialLabs/Tutorial35/Exam.cs)

```bash
dotnet test tests/TutorialLabs/TutorialLabs.csproj --filter "FullyQualifiedName~Tutorial35.Exam"
```

---

**Previous: [← Tutorial 34 — HTTP Connector](34-connector-http.md)** | **Next: [Tutorial 36 — Email Connector →](36-connector-email.md)**
