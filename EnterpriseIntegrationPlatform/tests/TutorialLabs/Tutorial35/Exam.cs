// ============================================================================
// Tutorial 35 – SFTP Connector (Exam)
// ============================================================================
// EIP Pattern: Connector
// E2E: Connection pool lifecycle, upload serialization roundtrip,
//      and SftpConnectorAdapter as IConnector with MockEndpoint.
// ============================================================================
using System.Text;
using EnterpriseIntegrationPlatform.Connector.Sftp;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial35;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_ConnectionPoolLifecycle()
    {
        await using var output = new MockEndpoint("exam-pool");
        var client = Substitute.For<ISftpClient>();
        client.IsConnected.Returns(true);
        var pool = Substitute.For<ISftpConnectionPool>();
        pool.AcquireAsync(Arg.Any<CancellationToken>()).Returns(client);

        var connector = new SftpConnector(
            pool,
            Options.Create(new SftpConnectorOptions { RootPath = "/test" }),
            NullLogger<SftpConnector>.Instance);

        var e1 = IntegrationEnvelope<string>.Create("data1", "src", "Upload");
        var e2 = IntegrationEnvelope<string>.Create("data2", "src", "Upload");

        await connector.UploadAsync(e1, "f1.txt", s => Encoding.UTF8.GetBytes(s), default);
        await connector.UploadAsync(e2, "f2.txt", s => Encoding.UTF8.GetBytes(s), default);

        await pool.Received(2).AcquireAsync(Arg.Any<CancellationToken>());
        pool.Received(2).Release(client);

        await output.PublishAsync(e1, "pool-lifecycle", default);
        await output.PublishAsync(e2, "pool-lifecycle", default);
        output.AssertReceivedOnTopic("pool-lifecycle", 2);
    }

    [Test]
    public async Task Challenge2_UploadSerializationRoundTrip()
    {
        await using var output = new MockEndpoint("exam-serial");
        byte[]? capturedBytes = null;

        var client = Substitute.For<ISftpClient>();
        client.IsConnected.Returns(true);
        client.When(c => c.UploadFile(
                Arg.Any<Stream>(), Arg.Is<string>(s => !s.EndsWith(".meta"))))
            .Do(ci =>
            {
                var stream = ci.ArgAt<Stream>(0);
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                capturedBytes = ms.ToArray();
            });

        var pool = Substitute.For<ISftpConnectionPool>();
        pool.AcquireAsync(Arg.Any<CancellationToken>()).Returns(client);

        var connector = new SftpConnector(
            pool,
            Options.Create(new SftpConnectorOptions()),
            NullLogger<SftpConnector>.Instance);

        var payload = "Hello SFTP World!";
        var envelope = IntegrationEnvelope<string>.Create(payload, "test", "Upload");
        await connector.UploadAsync(
            envelope, "hello.txt", s => Encoding.UTF8.GetBytes(s), default);

        Assert.That(capturedBytes, Is.Not.Null);
        Assert.That(Encoding.UTF8.GetString(capturedBytes!), Is.EqualTo(payload));

        await output.PublishAsync(envelope, "roundtrip", default);
        output.AssertReceivedOnTopic("roundtrip", 1);
    }

    [Test]
    public async Task Challenge3_AdapterImplementsIConnector()
    {
        await using var output = new MockEndpoint("exam-adapter");
        var sftpConnector = Substitute.For<ISftpConnector>();
        sftpConnector.UploadAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Any<string>(), Arg.Any<Func<string, byte[]>>(),
            Arg.Any<CancellationToken>())
            .Returns("/remote/data.json");

        var sftpClient = Substitute.For<ISftpClient>();
        sftpClient.IsConnected.Returns(true);

        var adapter = new SftpConnectorAdapter(
            "my-sftp", sftpConnector, sftpClient,
            NullLogger<SftpConnectorAdapter>.Instance);

        Assert.That(adapter.Name, Is.EqualTo("my-sftp"));
        Assert.That(adapter.ConnectorType, Is.EqualTo(ConnectorType.Sftp));

        var envelope = IntegrationEnvelope<string>.Create(
            "{\"key\":\"value\"}", "app", "Transfer");
        var result = await adapter.SendAsync(
            envelope, new ConnectorSendOptions { Destination = "data.json" });

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConnectorName, Is.EqualTo("my-sftp"));

        await output.PublishAsync(envelope, "adapter-results", default);
        output.AssertReceivedOnTopic("adapter-results", 1);
    }
}
