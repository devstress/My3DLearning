// ============================================================================
// Tutorial 37 – Connector.File (Lab)
// ============================================================================
// This lab exercises FileConnectorOptions, IFileSystem, PhysicalFileSystem,
// and FileConnector to learn the File connector subsystem.
// ============================================================================

using EnterpriseIntegrationPlatform.Connector.FileSystem;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial37;

[TestFixture]
public sealed class Lab
{
    // ── FileConnectorOptions Defaults ───────────────────────────────────────

    [Test]
    public void FileConnectorOptions_Defaults()
    {
        var opts = new FileConnectorOptions();

        Assert.That(opts.RootDirectory, Is.EqualTo(string.Empty));
        Assert.That(opts.Encoding, Is.EqualTo("utf-8"));
        Assert.That(opts.CreateDirectoryIfNotExists, Is.True);
        Assert.That(opts.OverwriteExisting, Is.False);
        Assert.That(opts.FilenamePattern, Is.EqualTo("{MessageId}-{MessageType}.json"));
    }

    // ── FileConnectorOptions Custom Values ──────────────────────────────────

    [Test]
    public void FileConnectorOptions_CustomValues()
    {
        var opts = new FileConnectorOptions
        {
            RootDirectory = "/data/exports",
            Encoding = "ascii",
            CreateDirectoryIfNotExists = false,
            OverwriteExisting = true,
            FilenamePattern = "{CorrelationId}.xml",
        };

        Assert.That(opts.RootDirectory, Is.EqualTo("/data/exports"));
        Assert.That(opts.Encoding, Is.EqualTo("ascii"));
        Assert.That(opts.CreateDirectoryIfNotExists, Is.False);
        Assert.That(opts.OverwriteExisting, Is.True);
        Assert.That(opts.FilenamePattern, Is.EqualTo("{CorrelationId}.xml"));
    }

    // ── IFileSystem Interface Shape (Reflection) ────────────────────────────

    [Test]
    public void IFileSystem_InterfaceShape_HasExpectedMembers()
    {
        var type = typeof(IFileSystem);

        Assert.That(type.GetMethod("WriteAllBytesAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("ReadAllBytesAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("GetFiles"), Is.Not.Null);
        Assert.That(type.GetMethod("FileExists"), Is.Not.Null);
        Assert.That(type.GetMethod("CreateDirectory"), Is.Not.Null);
    }

    // ── FileConnector Writes via Mocked IFileSystem ─────────────────────────

    [Test]
    public async Task FileConnector_Write_DelegatesToFileSystem()
    {
        var fs = Substitute.For<IFileSystem>();

        var opts = Options.Create(new FileConnectorOptions
        {
            RootDirectory = "/output",
            CreateDirectoryIfNotExists = true,
        });

        var connector = new FileConnector(fs, opts, NullLogger<FileConnector>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("payload", "Svc", "order.placed");

        await connector.WriteAsync(
            envelope,
            s => System.Text.Encoding.UTF8.GetBytes(s),
            CancellationToken.None);

        // Verify directory creation was called
        fs.Received(1).CreateDirectory(Arg.Any<string>());

        // Verify file write was called (data + metadata sidecar)
        await fs.Received(2).WriteAllBytesAsync(
            Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    // ── FileConnector Reads via Mocked IFileSystem ──────────────────────────

    [Test]
    public async Task FileConnector_Read_DelegatesToFileSystem()
    {
        var fs = Substitute.For<IFileSystem>();
        var expected = System.Text.Encoding.UTF8.GetBytes("file-content");
        fs.ReadAllBytesAsync("/output/test.json", Arg.Any<CancellationToken>())
            .Returns(expected);

        var connector = new FileConnector(
            fs,
            Options.Create(new FileConnectorOptions { RootDirectory = "/output" }),
            NullLogger<FileConnector>.Instance);

        var result = await connector.ReadAsync("/output/test.json", CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }

    // ── FileConnector Lists Files via Mocked IFileSystem ────────────────────

    [Test]
    public async Task FileConnector_ListFiles_DelegatesToFileSystem()
    {
        var fs = Substitute.For<IFileSystem>();
        fs.GetFiles(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new[] { "/output/a.json", "/output/b.json" });

        var connector = new FileConnector(
            fs,
            Options.Create(new FileConnectorOptions { RootDirectory = "/output" }),
            NullLogger<FileConnector>.Instance);

        var files = await connector.ListFilesAsync(null, "*.json", CancellationToken.None);

        Assert.That(files, Has.Count.EqualTo(2));
        Assert.That(files, Does.Contain("/output/a.json"));
    }

    // ── PhysicalFileSystem Implements IFileSystem ───────────────────────────

    [Test]
    public void PhysicalFileSystem_ImplementsIFileSystem()
    {
        var pfs = new PhysicalFileSystem();

        Assert.That(pfs, Is.InstanceOf<IFileSystem>());
    }
}
