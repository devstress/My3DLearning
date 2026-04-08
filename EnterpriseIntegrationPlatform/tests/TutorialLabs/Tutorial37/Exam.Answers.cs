// ============================================================================
// Tutorial 37 – File Connector (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — write and read roundtrip_ through mock endpoint
//   🟡 Intermediate — custom filename pattern_ contains message type and id
//   🔴 Advanced     — subdirectory listing_ combines root and sub
// ============================================================================

using System.Text;
using EnterpriseIntegrationPlatform.Connector.FileSystem;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial37;

[TestFixture]
public sealed class ExamAnswers
{
    [Test]
    public async Task Starter_WriteAndReadRoundtrip_ThroughMockEndpoint()
    {
        await using var input = new MockEndpoint("exam-file-in");

        var fs = new MockFileSystem();

        var connector = new FileConnector(fs,
            Options.Create(new FileConnectorOptions
            {
                RootDirectory = "/data",
                CreateDirectoryIfNotExists = true,
            }),
            NullLogger<FileConnector>.Instance);

        string? writtenPath = null;
        await input.SubscribeAsync<string>("file-topic", "file-group",
            async envelope =>
            {
                writtenPath = await connector.WriteAsync(
                    envelope, s => Encoding.UTF8.GetBytes(s), CancellationToken.None);
            });

        var payload = "roundtrip-test-payload";
        var env = IntegrationEnvelope<string>.Create(payload, "Svc", "test.roundtrip");
        await input.SendAsync(env);

        Assert.That(writtenPath, Is.Not.Null.And.Not.Empty);
        Assert.That(fs.Files.ContainsKey(writtenPath!), Is.True);

        var readBytes = await connector.ReadAsync(writtenPath!, CancellationToken.None);
        Assert.That(Encoding.UTF8.GetString(readBytes), Is.EqualTo(payload));
    }

    [Test]
    public async Task Intermediate_CustomFilenamePattern_ContainsMessageTypeAndId()
    {
        var fs = new MockFileSystem();

        var connector = new FileConnector(fs,
            Options.Create(new FileConnectorOptions
            {
                RootDirectory = "/exports",
                FilenamePattern = "{MessageType}-{MessageId}.json",
                CreateDirectoryIfNotExists = false,
            }),
            NullLogger<FileConnector>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "invoice.created");

        await connector.WriteAsync(envelope, s => Encoding.UTF8.GetBytes(s), CancellationToken.None);

        var capturedPath = fs.LastWrittenPath;
        Assert.That(capturedPath, Is.Not.Null);
        Assert.That(capturedPath, Does.Contain("invoice.created"));
        Assert.That(capturedPath, Does.Contain(envelope.MessageId.ToString()));
    }

    [Test]
    public async Task Advanced_SubdirectoryListing_CombinesRootAndSub()
    {
        var fs = new MockFileSystem();
        fs.Files["/root/sub/file1.json"] = Array.Empty<byte>();
        fs.Files["/root/sub/file2.json"] = Array.Empty<byte>();
        fs.Files["/root/sub/file3.json"] = Array.Empty<byte>();

        var connector = new FileConnector(fs,
            Options.Create(new FileConnectorOptions { RootDirectory = "/root" }),
            NullLogger<FileConnector>.Instance);

        var files = await connector.ListFilesAsync("sub", "*.json", CancellationToken.None);

        Assert.That(files, Has.Count.EqualTo(3));
        Assert.That(files[0], Does.Contain("sub"));
    }
}
