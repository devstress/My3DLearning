// ============================================================================
// Tutorial 37 – File Connector (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — write and read roundtrip_ through mock endpoint
//   🟡 Intermediate  — custom filename pattern_ contains message type and id
//   🔴 Advanced      — subdirectory listing_ combines root and sub
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using System.Text;
using EnterpriseIntegrationPlatform.Connector.FileSystem;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial37;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_WriteAndReadRoundtrip_ThroughMockEndpoint()
    {
        await using var input = new MockEndpoint("exam-file-in");

        // TODO: Create a MockFileSystem with appropriate configuration
        dynamic fs = null!;

        // TODO: Create a FileConnector with appropriate configuration
        dynamic connector = null!;

        string? writtenPath = null;
        await input.SubscribeAsync<string>("file-topic", "file-group",
            async envelope =>
            {
                writtenPath = await connector.WriteAsync(
                    envelope, s => Encoding.UTF8.GetBytes(s), CancellationToken.None);
            });

        var payload = "roundtrip-test-payload";
        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic env = null!;
        // TODO: await input.SendAsync(...)

        Assert.That(writtenPath, Is.Not.Null.And.Not.Empty);
        Assert.That(fs.Files.ContainsKey(writtenPath!), Is.True);

        // TODO: var readBytes = await connector.ReadAsync(...)
        dynamic readBytes = null!;
        Assert.That(Encoding.UTF8.GetString(readBytes), Is.EqualTo(payload));
    }

    [Test]
    public async Task Intermediate_CustomFilenamePattern_ContainsMessageTypeAndId()
    {
        // TODO: Create a MockFileSystem with appropriate configuration
        dynamic fs = null!;

        // TODO: Create a FileConnector with appropriate configuration
        dynamic connector = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;

        await connector.WriteAsync(envelope, s => Encoding.UTF8.GetBytes(s), CancellationToken.None);

        var capturedPath = fs.LastWrittenPath;
        Assert.That(capturedPath, Is.Not.Null);
        Assert.That(capturedPath, Does.Contain("invoice.created"));
        Assert.That(capturedPath, Does.Contain(envelope.MessageId.ToString()));
    }

    [Test]
    public async Task Advanced_SubdirectoryListing_CombinesRootAndSub()
    {
        // TODO: Create a MockFileSystem with appropriate configuration
        dynamic fs = null!;
        fs.Files["/root/sub/file1.json"] = Array.Empty<byte>();
        fs.Files["/root/sub/file2.json"] = Array.Empty<byte>();
        fs.Files["/root/sub/file3.json"] = Array.Empty<byte>();

        // TODO: Create a FileConnector with appropriate configuration
        dynamic connector = null!;

        // TODO: var files = await connector.ListFilesAsync(...)
        dynamic files = null!;

        Assert.That(files, Has.Count.EqualTo(3));
        Assert.That(files[0], Does.Contain("sub"));
    }
}
#endif
