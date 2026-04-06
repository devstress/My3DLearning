// ============================================================================
// Tutorial 35 – Connector.Sftp (Lab)
// ============================================================================
// This lab exercises SftpConnectorOptions, ISftpClient, SftpConnectionPool,
// SftpConnector, and ISftpConnector to learn the SFTP connector subsystem.
// ============================================================================

using EnterpriseIntegrationPlatform.Connector.Sftp;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial35;

[TestFixture]
public sealed class Lab
{
    // ── SftpConnectorOptions Defaults ────────────────────────────────────────

    [Test]
    public void SftpConnectorOptions_Defaults()
    {
        var opts = new SftpConnectorOptions();

        Assert.That(opts.Host, Is.EqualTo(string.Empty));
        Assert.That(opts.Port, Is.EqualTo(22));
        Assert.That(opts.Username, Is.EqualTo(string.Empty));
        Assert.That(opts.Password, Is.EqualTo(string.Empty));
        Assert.That(opts.RootPath, Is.EqualTo("/"));
        Assert.That(opts.TimeoutMs, Is.EqualTo(10000));
        Assert.That(opts.MaxConnectionsPerHost, Is.EqualTo(5));
    }

    // ── SftpConnectorOptions Custom Values ──────────────────────────────────

    [Test]
    public void SftpConnectorOptions_CustomValues()
    {
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
    }

    // ── ISftpClient Interface Shape (Reflection) ────────────────────────────

    [Test]
    public void ISftpClient_InterfaceShape_HasExpectedMethods()
    {
        var type = typeof(ISftpClient);

        Assert.That(type.GetMethod("Connect"), Is.Not.Null);
        Assert.That(type.GetMethod("Disconnect"), Is.Not.Null);
        Assert.That(type.GetMethod("UploadFile"), Is.Not.Null);
        Assert.That(type.GetMethod("DownloadFile"), Is.Not.Null);
        Assert.That(type.GetMethod("ListFiles"), Is.Not.Null);
        Assert.That(type.GetMethod("DeleteFile"), Is.Not.Null);
        Assert.That(type.GetProperty("IsConnected"), Is.Not.Null);
    }

    // ── SftpConnectionPool Acquires and Releases Mocked Client ──────────────

    [Test]
    public async Task SftpConnectionPool_AcquireAndRelease()
    {
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
    }

    // ── SftpConnector Upload Delegates to Pool ──────────────────────────────

    [Test]
    public async Task SftpConnector_Upload_DelegatesToPool()
    {
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
    }

    // ── ISftpConnector Interface Shape (Reflection) ─────────────────────────

    [Test]
    public void ISftpConnector_InterfaceShape()
    {
        var type = typeof(ISftpConnector);

        Assert.That(type.GetMethod("UploadAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("DownloadAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("ListFilesAsync"), Is.Not.Null);
    }

    // ── SftpConnectionPool Respects Max Connections ─────────────────────────

    [Test]
    public async Task SftpConnectionPool_RespectsMaxConnections()
    {
        var clientCount = 0;
        ISftpClient CreateClient()
        {
            Interlocked.Increment(ref clientCount);
            var client = Substitute.For<ISftpClient>();
            client.IsConnected.Returns(true);
            return client;
        }

        var pool = new SftpConnectionPool(
            CreateClient,
            Options.Create(new SftpConnectorOptions { MaxConnectionsPerHost = 2 }),
            NullLogger<SftpConnectionPool>.Instance);

        // Acquire both slots
        var c1 = await pool.AcquireAsync();
        var c2 = await pool.AcquireAsync();

        // Third acquire should block; verify with a timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        Assert.ThrowsAsync<OperationCanceledException>(
            () => pool.AcquireAsync(cts.Token));

        // Release one, then acquire succeeds
        pool.Release(c1);
        var c3 = await pool.AcquireAsync();
        Assert.That(c3, Is.Not.Null);

        pool.Release(c2);
        pool.Release(c3);
        await pool.DisposeAsync();
    }
}
