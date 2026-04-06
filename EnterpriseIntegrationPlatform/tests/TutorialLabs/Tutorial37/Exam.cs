// ============================================================================
// Tutorial 37 – Connector.File (Exam)
// ============================================================================
// Coding challenges: write-read roundtrip, custom filename pattern,
// and directory creation when CreateDirectoryIfNotExists is true.
// ============================================================================

using EnterpriseIntegrationPlatform.Connector.FileSystem;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial37;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Write and Read Roundtrip ───────────────────────────────

    [Test]
    public async Task Challenge1_WriteAndReadRoundtrip_WithMockFileSystem()
    {
        var store = new Dictionary<string, byte[]>();
        var fs = Substitute.For<IFileSystem>();

        fs.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => store[ci.ArgAt<string>(0)] = ci.ArgAt<byte[]>(1));

        fs.ReadAllBytesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(store[ci.ArgAt<string>(0)]));

        var opts = Options.Create(new FileConnectorOptions
        {
            RootDirectory = "/data",
            CreateDirectoryIfNotExists = true,
        });

        var connector = new FileConnector(fs, opts, NullLogger<FileConnector>.Instance);

        var payload = "roundtrip-test-payload";
        var envelope = IntegrationEnvelope<string>.Create(payload, "Svc", "test.roundtrip");

        var writtenPath = await connector.WriteAsync(
            envelope,
            s => System.Text.Encoding.UTF8.GetBytes(s),
            CancellationToken.None);

        Assert.That(writtenPath, Is.Not.Null.And.Not.Empty);
        Assert.That(store.ContainsKey(writtenPath), Is.True);

        var readBytes = await connector.ReadAsync(writtenPath, CancellationToken.None);
        var readPayload = System.Text.Encoding.UTF8.GetString(readBytes);

        Assert.That(readPayload, Is.EqualTo(payload));
    }

    // ── Challenge 2: Custom Filename Pattern Resolution ─────────────────────

    [Test]
    public async Task Challenge2_CustomFilenamePatternResolution()
    {
        string? capturedPath = null;
        var fs = Substitute.For<IFileSystem>();
        fs.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci =>
            {
                var path = ci.ArgAt<string>(0);
                if (!path.EndsWith(".meta"))
                    capturedPath = path;
            });

        var opts = Options.Create(new FileConnectorOptions
        {
            RootDirectory = "/exports",
            FilenamePattern = "{MessageType}-{MessageId}.json",
            CreateDirectoryIfNotExists = false,
        });

        var connector = new FileConnector(fs, opts, NullLogger<FileConnector>.Instance);
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "invoice.created");

        await connector.WriteAsync(
            envelope,
            s => System.Text.Encoding.UTF8.GetBytes(s),
            CancellationToken.None);

        Assert.That(capturedPath, Is.Not.Null);
        Assert.That(capturedPath, Does.Contain("invoice.created"));
        Assert.That(capturedPath, Does.Contain(envelope.MessageId.ToString()));
    }

    // ── Challenge 3: Directory Creation When CreateDirectoryIfNotExists ─────

    [Test]
    public async Task Challenge3_DirectoryCreation_WhenOptionEnabled()
    {
        var fs = Substitute.For<IFileSystem>();

        var opts = Options.Create(new FileConnectorOptions
        {
            RootDirectory = "/new-dir/sub",
            CreateDirectoryIfNotExists = true,
        });

        var connector = new FileConnector(fs, opts, NullLogger<FileConnector>.Instance);
        var envelope = IntegrationEnvelope<string>.Create("content", "Svc", "event.new");

        await connector.WriteAsync(
            envelope,
            s => System.Text.Encoding.UTF8.GetBytes(s),
            CancellationToken.None);

        fs.Received(1).CreateDirectory(Arg.Is<string>(p => p.Contains("/new-dir/sub")));
    }
}
