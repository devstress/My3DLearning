// ============================================================================
// Tutorial 35 – SFTP Connector (Lab)
// ============================================================================
// EIP Pattern: Connector
// E2E: SftpConnector with MockSftpConnectionPool/MockSftpClient +
//      MockEndpoint for publishing transfer results.
// ============================================================================
using System.Text;
using EnterpriseIntegrationPlatform.Connector.Sftp;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial35;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("sftp-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    private static (MockSftpConnectionPool Pool, MockSftpClient Client) CreateMockPool()
    {
        var client = new MockSftpClient();
        var pool = new MockSftpConnectionPool(client);
        return (pool, client);
    }

    private static SftpConnector CreateConnector(
        ISftpConnectionPool pool, string rootPath = "/") =>
        new(pool,
            Options.Create(new SftpConnectorOptions { RootPath = rootPath }),
            NullLogger<SftpConnector>.Instance);


    // ── 1. Configuration Defaults ────────────────────────────────────

    [Test]
    public async Task SftpConnectorOptions_Defaults()
    {
        var opts = new SftpConnectorOptions();

        Assert.That(opts.Port, Is.EqualTo(22));
        Assert.That(opts.RootPath, Is.EqualTo("/"));
        Assert.That(opts.TimeoutMs, Is.EqualTo(10000));
        Assert.That(opts.MaxConnectionsPerHost, Is.EqualTo(5));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Upload_DelegatesToPoolAndClient()
    {
        var (pool, client) = CreateMockPool();
        var connector = CreateConnector(pool, "/uploads");

        var envelope = IntegrationEnvelope<string>.Create("file-content", "src", "Upload");
        var path = await connector.UploadAsync(
            envelope, "test.dat", s => Encoding.UTF8.GetBytes(s), default);

        Assert.That(path, Does.Contain("test.dat"));
        Assert.That(client.UploadedPaths.Any(p => p.Contains("test.dat")), Is.True);

        await _output.PublishAsync(envelope, "upload-results", default);
        _output.AssertReceivedOnTopic("upload-results", 1);
    }


    // ── 2. Core File Operations ──────────────────────────────────────

    [Test]
    public async Task Download_DelegatesToPoolAndClient()
    {
        var (pool, client) = CreateMockPool();
        var data = Encoding.UTF8.GetBytes("downloaded-content");
        client.Files["/remote/file.txt"] = data;

        var connector = CreateConnector(pool);
        var result = await connector.DownloadAsync("/remote/file.txt", default);

        Assert.That(Encoding.UTF8.GetString(result), Is.EqualTo("downloaded-content"));
    }

    [Test]
    public async Task ListFiles_DelegatesToPoolAndClient()
    {
        var (pool, client) = CreateMockPool();
        client.Files["/data/a.txt"] = Array.Empty<byte>();
        client.Files["/data/b.txt"] = Array.Empty<byte>();

        var connector = CreateConnector(pool);
        var files = await connector.ListFilesAsync("/data", default);

        Assert.That(files, Has.Count.EqualTo(2));
        Assert.That(files, Does.Contain("/data/a.txt"));
    }

    [Test]
    public async Task Upload_CreatesMetadataSidecar()
    {
        var (pool, client) = CreateMockPool();
        var connector = CreateConnector(pool, "/out");

        var envelope = IntegrationEnvelope<string>.Create("payload", "src", "Doc");
        await connector.UploadAsync(
            envelope, "doc.json", s => Encoding.UTF8.GetBytes(s), default);

        Assert.That(client.UploadCount, Is.EqualTo(2));
        Assert.That(client.UploadedPaths.Any(p => p.EndsWith(".meta")), Is.True);
    }


    // ── 3. Upload Lifecycle ──────────────────────────────────────────

    [Test]
    public async Task PoolRelease_CalledAfterUpload()
    {
        var (pool, client) = CreateMockPool();
        var connector = CreateConnector(pool);

        var envelope = IntegrationEnvelope<string>.Create("data", "src", "File");
        await connector.UploadAsync(
            envelope, "file.bin", s => Encoding.UTF8.GetBytes(s), default);

        Assert.That(pool.ReleaseCount, Is.EqualTo(1));
    }

    [Test]
    public async Task UploadResult_PublishedToMockEndpoint()
    {
        var (pool, _) = CreateMockPool();
        var connector = CreateConnector(pool, "/files");

        var envelope = IntegrationEnvelope<string>.Create("content", "app", "Report");
        var remotePath = await connector.UploadAsync(
            envelope, "report.csv", s => Encoding.UTF8.GetBytes(s), default);

        var notification = IntegrationEnvelope<string>.Create(
            remotePath, "sftp", "UploadComplete");
        await _output.PublishAsync(notification, "sftp-notifications", default);
        _output.AssertReceivedOnTopic("sftp-notifications", 1);

        var received = _output.GetReceived<string>();
        Assert.That(received.Payload, Does.Contain("report.csv"));
    }
}
