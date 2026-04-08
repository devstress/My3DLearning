// ============================================================================
// Tutorial 35 – SFTP Connector (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — connection pool lifecycle
//   🟡 Intermediate  — upload serialization round trip
//   🔴 Advanced      — adapter implements i connector
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using System.Text;
using EnterpriseIntegrationPlatform.Connector.Sftp;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial35;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_ConnectionPoolLifecycle()
    {
        await using var output = new MockEndpoint("exam-pool");
        // TODO: Create a MockSftpClient with appropriate configuration
        dynamic client = null!;
        // TODO: Create a MockSftpConnectionPool with appropriate configuration
        dynamic pool = null!;

        // TODO: Create a SftpConnector with appropriate configuration
        dynamic connector = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic e1 = null!;
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic e2 = null!;

        await connector.UploadAsync(e1, "f1.txt", s => Encoding.UTF8.GetBytes(s), default);
        await connector.UploadAsync(e2, "f2.txt", s => Encoding.UTF8.GetBytes(s), default);

        Assert.That(pool.AcquireCount, Is.EqualTo(2));
        Assert.That(pool.ReleaseCount, Is.EqualTo(2));

        // TODO: await output.PublishAsync(...)
        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("pool-lifecycle", 2);
    }

    [Test]
    public async Task Intermediate_UploadSerializationRoundTrip()
    {
        await using var output = new MockEndpoint("exam-serial");

        // TODO: Create a MockSftpClient with appropriate configuration
        dynamic client = null!;
        // TODO: Create a MockSftpConnectionPool with appropriate configuration
        dynamic pool = null!;

        // TODO: Create a SftpConnector with appropriate configuration
        dynamic connector = null!;

        var payload = "Hello SFTP World!";
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        await connector.UploadAsync(
            envelope, "hello.txt", s => Encoding.UTF8.GetBytes(s), default);

        var dataPath = client.UploadedPaths.First(p => !p.EndsWith(".meta"));
        var capturedBytes = client.Files[dataPath];

        Assert.That(capturedBytes, Is.Not.Null);
        Assert.That(Encoding.UTF8.GetString(capturedBytes), Is.EqualTo(payload));

        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("roundtrip", 1);
    }

    [Test]
    public async Task Advanced_AdapterImplementsIConnector()
    {
        await using var output = new MockEndpoint("exam-adapter");
        // TODO: Create a MockSftpClient with appropriate configuration
        dynamic client = null!;
        client.Connect();
        // TODO: Create a MockSftpConnector with appropriate configuration
        dynamic sftpConnector = null!;

        // TODO: Create a SftpConnectorAdapter with appropriate configuration
        dynamic adapter = null!;

        Assert.That(adapter.Name, Is.EqualTo("my-sftp"));
        Assert.That(adapter.ConnectorType, Is.EqualTo(ConnectorType.Sftp));

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: var result = await adapter.SendAsync(...)
        dynamic result = null!;

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConnectorName, Is.EqualTo("my-sftp"));

        // TODO: await output.PublishAsync(...)
        output.AssertReceivedOnTopic("adapter-results", 1);
    }
}
#endif
