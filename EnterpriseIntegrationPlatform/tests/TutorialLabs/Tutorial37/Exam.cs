// ============================================================================
// Tutorial 37 – File Connector (Exam)
// ============================================================================
// E2E challenges: write-read roundtrip through MockEndpoint, custom filename
// pattern resolution, and subdirectory listing.
// ============================================================================

using System.Text;
using EnterpriseIntegrationPlatform.Connector.FileSystem;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial37;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_WriteAndReadRoundtrip_ThroughMockEndpoint()
    {
        await using var input = new MockEndpoint("exam-file-in");

        var store = new Dictionary<string, byte[]>();
        var fs = Substitute.For<IFileSystem>();
        fs.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => store[ci.ArgAt<string>(0)] = ci.ArgAt<byte[]>(1));
        fs.ReadAllBytesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(store[ci.ArgAt<string>(0)]));

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
        Assert.That(store.ContainsKey(writtenPath!), Is.True);

        var readBytes = await connector.ReadAsync(writtenPath!, CancellationToken.None);
        Assert.That(Encoding.UTF8.GetString(readBytes), Is.EqualTo(payload));
    }

    [Test]
    public async Task Challenge2_CustomFilenamePattern_ContainsMessageTypeAndId()
    {
        string? capturedPath = null;
        var fs = Substitute.For<IFileSystem>();
        fs.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci =>
            {
                var path = ci.ArgAt<string>(0);
                if (!path.EndsWith(".meta.json"))
                    capturedPath = path;
            });

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

        Assert.That(capturedPath, Is.Not.Null);
        Assert.That(capturedPath, Does.Contain("invoice.created"));
        Assert.That(capturedPath, Does.Contain(envelope.MessageId.ToString()));
    }

    [Test]
    public async Task Challenge3_SubdirectoryListing_CombinesRootAndSub()
    {
        var fs = Substitute.For<IFileSystem>();
        fs.GetFiles(Arg.Is<string>(d => d.Contains("sub")), Arg.Any<string>())
            .Returns(new[] { "/root/sub/file1.json", "/root/sub/file2.json", "/root/sub/file3.json" });

        var connector = new FileConnector(fs,
            Options.Create(new FileConnectorOptions { RootDirectory = "/root" }),
            NullLogger<FileConnector>.Instance);

        var files = await connector.ListFilesAsync("sub", "*.json", CancellationToken.None);

        Assert.That(files, Has.Count.EqualTo(3));
        Assert.That(files[0], Does.Contain("sub"));
    }
}
