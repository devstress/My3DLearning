// ============================================================================
// Tutorial 35 – SFTP Connector (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — connection pool lifecycle
//   🟡 Intermediate — upload serialization round trip
//   🔴 Advanced     — adapter implements i connector
// ============================================================================

using System.Text;
using EnterpriseIntegrationPlatform.Connector.Sftp;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial35;

[TestFixture]
public sealed class ExamAnswers
{
    [Test]
    public async Task Starter_ConnectionPoolLifecycle()
    {
        await using var output = new MockEndpoint("exam-pool");
        var client = new MockSftpClient();
        var pool = new MockSftpConnectionPool(client);

        var connector = new SftpConnector(
            pool,
            Options.Create(new SftpConnectorOptions { RootPath = "/test" }),
            NullLogger<SftpConnector>.Instance);

        var e1 = IntegrationEnvelope<string>.Create("data1", "src", "Upload");
        var e2 = IntegrationEnvelope<string>.Create("data2", "src", "Upload");

        await connector.UploadAsync(e1, "f1.txt", s => Encoding.UTF8.GetBytes(s), default);
        await connector.UploadAsync(e2, "f2.txt", s => Encoding.UTF8.GetBytes(s), default);

        Assert.That(pool.AcquireCount, Is.EqualTo(2));
        Assert.That(pool.ReleaseCount, Is.EqualTo(2));

        await output.PublishAsync(e1, "pool-lifecycle", default);
        await output.PublishAsync(e2, "pool-lifecycle", default);
        output.AssertReceivedOnTopic("pool-lifecycle", 2);
    }

    [Test]
    public async Task Intermediate_UploadSerializationRoundTrip()
    {
        await using var output = new MockEndpoint("exam-serial");

        var client = new MockSftpClient();
        var pool = new MockSftpConnectionPool(client);

        var connector = new SftpConnector(
            pool,
            Options.Create(new SftpConnectorOptions()),
            NullLogger<SftpConnector>.Instance);

        var payload = "Hello SFTP World!";
        var envelope = IntegrationEnvelope<string>.Create(payload, "test", "Upload");
        await connector.UploadAsync(
            envelope, "hello.txt", s => Encoding.UTF8.GetBytes(s), default);

        var dataPath = client.UploadedPaths.First(p => !p.EndsWith(".meta"));
        var capturedBytes = client.Files[dataPath];

        Assert.That(capturedBytes, Is.Not.Null);
        Assert.That(Encoding.UTF8.GetString(capturedBytes), Is.EqualTo(payload));

        await output.PublishAsync(envelope, "roundtrip", default);
        output.AssertReceivedOnTopic("roundtrip", 1);
    }

    [Test]
    public async Task Advanced_AdapterImplementsIConnector()
    {
        await using var output = new MockEndpoint("exam-adapter");
        var client = new MockSftpClient();
        client.Connect();
        var sftpConnector = new MockSftpConnector(client);

        var adapter = new SftpConnectorAdapter(
            "my-sftp", sftpConnector, client,
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
