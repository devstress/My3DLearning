using EnterpriseIntegrationPlatform.Connector.Sftp;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class SftpConnectorTests
{
    private ISftpClient _sftpClient = null!;
#pragma warning disable NUnit1032 // _pool is a mock — no disposal needed
    private ISftpConnectionPool _pool = null!;
#pragma warning restore NUnit1032

    [SetUp]
    public void SetUp()
    {
        _sftpClient = Substitute.For<ISftpClient>();
        _sftpClient.IsConnected.Returns(true);
        _pool = Substitute.For<ISftpConnectionPool>();
        _pool.AcquireAsync(Arg.Any<CancellationToken>()).Returns(_sftpClient);
    }

    private SftpConnector BuildConnector(string rootPath = "/uploads") =>
        new SftpConnector(
            _pool,
            Options.Create(new SftpConnectorOptions { RootPath = rootPath }),
            NullLogger<SftpConnector>.Instance);

    private static IntegrationEnvelope<string> BuildEnvelope(string payload = "test-data") =>
        IntegrationEnvelope<string>.Create(payload, "TestService", "TestEvent");

    private static byte[] Utf8Bytes(string s) => Encoding.UTF8.GetBytes(s);

    [Test]
    public async Task UploadAsync_ValidEnvelope_UploadsToCorrectRemotePath()
    {
        var connector = BuildConnector("/files");
        var envelope = BuildEnvelope("payload");
        string? capturedPath = null;
        _sftpClient
            .When(c => c.UploadFile(Arg.Any<Stream>(), Arg.Any<string>()))
            .Do(ci => capturedPath ??= (string)ci[1]);

        await connector.UploadAsync(envelope, "data.json", Utf8Bytes, CancellationToken.None);

        Assert.That(capturedPath, Is.EqualTo("/files/data.json"));
    }

    [Test]
    public async Task UploadAsync_ValidEnvelope_WritesMetadataFile()
    {
        var connector = BuildConnector("/files");
        var envelope = BuildEnvelope();
        var uploadedPaths = new List<string>();
        _sftpClient
            .When(c => c.UploadFile(Arg.Any<Stream>(), Arg.Any<string>()))
            .Do(ci => uploadedPaths.Add((string)ci[1]));

        await connector.UploadAsync(envelope, "data.json", Utf8Bytes, CancellationToken.None);

        Assert.That(uploadedPaths, Does.Contain("/files/data.json.meta"));
    }

    [Test]
    public async Task DownloadAsync_ValidPath_ReturnsFileBytes()
    {
        var connector = BuildConnector();
        var expected = Encoding.UTF8.GetBytes("file-content");
        _sftpClient.DownloadFile("/uploads/file.json")
            .Returns(new MemoryStream(expected));

        var result = await connector.DownloadAsync("/uploads/file.json", CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task ListFilesAsync_ValidPath_ReturnsFiles()
    {
        var connector = BuildConnector();
        _sftpClient.ListFiles("/uploads").Returns(new[] { "/uploads/a.json", "/uploads/b.json" });

        var result = await connector.ListFilesAsync("/uploads", CancellationToken.None);

        Assert.That(result, Is.EquivalentTo(new[] { "/uploads/a.json", "/uploads/b.json" }));
    }

    [Test]
    public void UploadAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var connector = BuildConnector();

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await connector.UploadAsync<string>(null!, "file.json", Utf8Bytes, CancellationToken.None));
    }

    [Test]
    public async Task UploadAsync_AcquiresAndReleasesPool()
    {
        var connector = BuildConnector();
        var envelope = BuildEnvelope();

        await connector.UploadAsync(envelope, "data.json", Utf8Bytes, CancellationToken.None);

        await _pool.Received(1).AcquireAsync(Arg.Any<CancellationToken>());
        _pool.Received(1).Release(_sftpClient);
    }

    [Test]
    public async Task UploadAsync_ReleasesPoolOnException()
    {
        var connector = BuildConnector();
        var envelope = BuildEnvelope();
        _sftpClient
            .When(c => c.UploadFile(Arg.Any<Stream>(), Arg.Is<string>(p => !p.EndsWith(".meta"))))
            .Do(_ => throw new InvalidOperationException("simulated SFTP error"));

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await connector.UploadAsync(envelope, "data.json", Utf8Bytes, CancellationToken.None));
        _pool.Received(1).Release(_sftpClient);
    }

    [Test]
    public async Task DownloadAsync_AcquiresAndReleasesPool()
    {
        var connector = BuildConnector();
        _sftpClient.DownloadFile(Arg.Any<string>()).Returns(new MemoryStream());

        await connector.DownloadAsync("/uploads/f.json", CancellationToken.None);

        await _pool.Received(1).AcquireAsync(Arg.Any<CancellationToken>());
        _pool.Received(1).Release(_sftpClient);
    }

    [Test]
    public async Task DownloadAsync_ReturnsCorrectBytes()
    {
        var connector = BuildConnector();
        var expected = new byte[] { 0x42, 0x43, 0x44 };
        _sftpClient.DownloadFile("/uploads/binary.bin").Returns(new MemoryStream(expected));

        var result = await connector.DownloadAsync("/uploads/binary.bin", CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }
}

[TestFixture]
public class SftpConnectionPoolTests
{
    private ISftpClient _mockClient = null!;
    private SftpConnectionPool _pool = null!;

    [SetUp]
    public void SetUp()
    {
        _mockClient = Substitute.For<ISftpClient>();
        _mockClient.IsConnected.Returns(true);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_pool is not null)
            await _pool.DisposeAsync();
    }

    private SftpConnectionPool BuildPool(int maxConns = 2, int idleMs = 30000)
    {
        _pool = new SftpConnectionPool(
            () =>
            {
                var client = Substitute.For<ISftpClient>();
                client.IsConnected.Returns(true);
                return client;
            },
            Options.Create(new SftpConnectorOptions
            {
                MaxConnectionsPerHost = maxConns,
                ConnectionIdleTimeoutMs = idleMs,
            }),
            NullLogger<SftpConnectionPool>.Instance);
        return _pool;
    }

    [Test]
    public async Task Acquire_ReturnsConnectedClient()
    {
        var pool = BuildPool();
        var client = await pool.AcquireAsync();

        Assert.That(client, Is.Not.Null);
        Assert.That(client.IsConnected, Is.True);
    }

    [Test]
    public async Task Acquire_AfterRelease_ReusesConnection()
    {
        var pool = BuildPool(maxConns: 1);

        var first = await pool.AcquireAsync();
        pool.Release(first);

        var second = await pool.AcquireAsync();
        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public async Task Acquire_MaxReached_BlocksUntilReleased()
    {
        var pool = BuildPool(maxConns: 1);
        var first = await pool.AcquireAsync();

        // Second acquire should block until first is released.
        var acquireTask = pool.AcquireAsync(CancellationToken.None);
        Assert.That(acquireTask.IsCompleted, Is.False);

        pool.Release(first);

        var second = await acquireTask;
        Assert.That(second, Is.Not.Null);
    }

    [Test]
    public async Task Acquire_CancellationToken_ThrowsWhenCancelled()
    {
        var pool = BuildPool(maxConns: 1);
        var first = await pool.AcquireAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await pool.AcquireAsync(cts.Token));

        pool.Release(first); // cleanup
    }

    [Test]
    public async Task Release_DisconnectedClient_NotReturnedToPool()
    {
        var pool = BuildPool(maxConns: 1);
        var client = await pool.AcquireAsync();

        // Simulate a disconnected client
        client.IsConnected.Returns(false);
        pool.Release(client);

        // Next acquire should get a NEW client, not the disconnected one.
        var second = await pool.AcquireAsync();
        Assert.That(second, Is.Not.SameAs(client));
    }

    [Test]
    public async Task Acquire_ExpiredIdleConnection_CreatesNew()
    {
        var pool = BuildPool(maxConns: 1, idleMs: 1); // 1ms idle timeout

        var first = await pool.AcquireAsync();
        pool.Release(first);

        // Wait for idle timeout to expire
        await Task.Delay(20);

        var second = await pool.AcquireAsync();
        Assert.That(second, Is.Not.SameAs(first));
    }

    [Test]
    public async Task DisposeAsync_DisposesIdleConnections()
    {
        var disposableClient = Substitute.For<ISftpClient, IDisposable>();
        disposableClient.IsConnected.Returns(true);
        var pool = new SftpConnectionPool(
            () => disposableClient,
            Options.Create(new SftpConnectorOptions { MaxConnectionsPerHost = 1 }),
            NullLogger<SftpConnectionPool>.Instance);

        var client = await pool.AcquireAsync();
        pool.Release(client);

        await pool.DisposeAsync();

        ((IDisposable)disposableClient).Received().Dispose();
        _pool = null!; // Prevent double-dispose in TearDown
    }

    [Test]
    public async Task Options_DefaultValues_AreCorrect()
    {
        var options = new SftpConnectorOptions();
        Assert.That(options.MaxConnectionsPerHost, Is.EqualTo(5));
        Assert.That(options.ConnectionIdleTimeoutMs, Is.EqualTo(30_000));
    }
}
