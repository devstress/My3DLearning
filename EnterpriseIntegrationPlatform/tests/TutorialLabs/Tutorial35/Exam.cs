// ============================================================================
// Tutorial 35 – Connector.Sftp (Exam)
// ============================================================================
// Coding challenges: connection pool lifecycle, upload serialization
// roundtrip with a mock, and SftpConnectorAdapter as IConnector.
// ============================================================================

using EnterpriseIntegrationPlatform.Connector.Sftp;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial35;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Connection Pool Lifecycle ──────────────────────────────

    [Test]
    public async Task Challenge1_ConnectionPoolLifecycle_AcquireUseRelease()
    {
        var mockClient = Substitute.For<ISftpClient>();
        mockClient.IsConnected.Returns(true);

        var pool = new SftpConnectionPool(
            () => mockClient,
            Options.Create(new SftpConnectorOptions { MaxConnectionsPerHost = 3 }),
            NullLogger<SftpConnectionPool>.Instance);

        // Acquire
        var client = await pool.AcquireAsync();
        mockClient.Received(1).Connect();

        // Use: upload a file
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("data"));
        client.UploadFile(stream, "/remote/file.txt");
        mockClient.Received(1).UploadFile(Arg.Any<Stream>(), "/remote/file.txt");

        // Release back to pool
        pool.Release(client);

        // Re-acquire should reuse the pooled connection (no new Connect)
        var client2 = await pool.AcquireAsync();
        Assert.That(client2, Is.SameAs(mockClient));
        // Connect is still 1 because the idle connection was reused
        mockClient.Received(1).Connect();

        pool.Release(client2);
        await pool.DisposeAsync();
    }

    // ── Challenge 2: Upload Serialization Roundtrip ─────────────────────────

    [Test]
    public async Task Challenge2_UploadSerialization_RoundtripWithMock()
    {
        byte[]? capturedData = null;
        string? capturedPath = null;

        var mockClient = Substitute.For<ISftpClient>();
        mockClient.IsConnected.Returns(true);
        mockClient.When(c => c.UploadFile(Arg.Any<Stream>(), Arg.Any<string>()))
            .Do(callInfo =>
            {
                var s = callInfo.ArgAt<Stream>(0);
                var path = callInfo.ArgAt<string>(1);
                // Capture the first upload (data file, not .meta sidecar)
                if (capturedData is null && !path.EndsWith(".meta"))
                {
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    capturedData = ms.ToArray();
                    capturedPath = path;
                }
            });

        var mockPool = Substitute.For<ISftpConnectionPool>();
        mockPool.AcquireAsync(Arg.Any<CancellationToken>()).Returns(mockClient);

        var connector = new SftpConnector(
            mockPool,
            Options.Create(new SftpConnectorOptions { RootPath = "/exports" }),
            NullLogger<SftpConnector>.Instance);

        var payload = "Hello, SFTP World!";
        var envelope = IntegrationEnvelope<string>.Create(payload, "Svc", "export.file");

        await connector.UploadAsync(
            envelope,
            "export.json",
            static s => System.Text.Encoding.UTF8.GetBytes(s),
            CancellationToken.None);

        Assert.That(capturedPath, Is.EqualTo("/exports/export.json"));
        Assert.That(capturedData, Is.Not.Null);
        Assert.That(System.Text.Encoding.UTF8.GetString(capturedData!), Is.EqualTo(payload));
    }

    // ── Challenge 3: SftpConnectorAdapter as IConnector ─────────────────────

    [Test]
    public async Task Challenge3_SftpConnectorAdapter_AsIConnector()
    {
        var mockSftpConnector = Substitute.For<ISftpConnector>();
        mockSftpConnector
            .UploadAsync(
                Arg.Any<IntegrationEnvelope<string>>(),
                Arg.Any<string>(),
                Arg.Any<Func<string, byte[]>>(),
                Arg.Any<CancellationToken>())
            .Returns("/remote/file.json");

        var mockClient = Substitute.For<ISftpClient>();

        var adapter = new SftpConnectorAdapter(
            "vendor-sftp",
            mockSftpConnector,
            mockClient,
            NullLogger<SftpConnectorAdapter>.Instance);

        // Verify IConnector interface properties
        Assert.That(adapter.Name, Is.EqualTo("vendor-sftp"));
        Assert.That(adapter.ConnectorType, Is.EqualTo(ConnectorType.Sftp));

        // Use the adapter via IConnector.SendAsync
        IConnector connector = adapter;
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "event");
        var result = await connector.SendAsync(
            envelope,
            new ConnectorSendOptions { Destination = "file.json" },
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConnectorName, Is.EqualTo("vendor-sftp"));
    }
}
