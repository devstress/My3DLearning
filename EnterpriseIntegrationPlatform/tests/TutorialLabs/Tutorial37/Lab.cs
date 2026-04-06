// ============================================================================
// Tutorial 37 – File Connector (Lab)
// ============================================================================
// EIP Pattern: Connector.
// E2E: Wire real FileConnector with MockFileSystem and
// MockEndpoint to simulate envelope-driven file I/O.
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
public sealed class Lab
{
    private MockEndpoint _input = null!;
    private MockFileSystem _fs = null!;

    [SetUp]
    public void SetUp()
    {
        _input = new MockEndpoint("file-in");
        _fs = new MockFileSystem();
    }

    [TearDown]
    public async Task TearDown() => await _input.DisposeAsync();

    [Test]
    public async Task Write_CreatesDataFile_AndMetadataSidecar()
    {
        var connector = CreateConnector();
        var envelope = IntegrationEnvelope<string>.Create("payload", "Svc", "order.placed");

        await connector.WriteAsync(envelope, s => Encoding.UTF8.GetBytes(s), CancellationToken.None);

        Assert.That(_fs.Calls.Count(c => c.Operation == "CreateDirectory"), Is.EqualTo(1));
        Assert.That(_fs.Calls.Count(c => c.Operation == "WriteAllBytes"), Is.EqualTo(2));
    }

    [Test]
    public async Task Write_ExpandsFilenamePattern_FromEnvelope()
    {
        var connector = CreateConnector();
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "invoice.created");

        await connector.WriteAsync(envelope, s => Encoding.UTF8.GetBytes(s), CancellationToken.None);

        var capturedPath = _fs.LastWrittenPath;
        Assert.That(capturedPath, Is.Not.Null);
        Assert.That(capturedPath, Does.Contain(envelope.MessageId.ToString()));
        Assert.That(capturedPath, Does.Contain("invoice.created"));
    }

    [Test]
    public async Task Write_CreatesDirectory_WhenOptionEnabled()
    {
        var connector = CreateConnector();
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "test.event");

        await connector.WriteAsync(envelope, s => Encoding.UTF8.GetBytes(s), CancellationToken.None);

        Assert.That(_fs.Calls.Count(c => c.Operation == "CreateDirectory" && c.Path == "/output"), Is.EqualTo(1));
    }

    [Test]
    public async Task Write_Throws_WhenFileExists_AndOverwriteDisabled()
    {
        var connector = new FileConnector(_fs,
            Options.Create(new FileConnectorOptions
            {
                RootDirectory = "/output",
                CreateDirectoryIfNotExists = true,
                OverwriteExisting = false,
            }),
            NullLogger<FileConnector>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "test.event");

        // Pre-populate the file so FileExists returns true for the generated path
        var expectedPath = Path.Combine("/output", $"{envelope.MessageId}-test.event.json");
        _fs.Files[expectedPath] = Array.Empty<byte>();

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await connector.WriteAsync(envelope, s => Encoding.UTF8.GetBytes(s), CancellationToken.None));
    }

    [Test]
    public async Task Read_ReturnsFileContent()
    {
        var expected = Encoding.UTF8.GetBytes("file-content");
        _fs.Files["/output/test.json"] = expected;

        var connector = CreateConnector();
        var result = await connector.ReadAsync("/output/test.json", CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task ListFiles_ReturnsMatchingPaths()
    {
        _fs.Files["/output/a.json"] = Array.Empty<byte>();
        _fs.Files["/output/b.json"] = Array.Empty<byte>();

        var connector = CreateConnector();
        var files = await connector.ListFilesAsync(null, "*.json", CancellationToken.None);

        Assert.That(files, Has.Count.EqualTo(2));
        Assert.That(files, Does.Contain("/output/a.json"));
    }

    [Test]
    public async Task E2E_MockEndpoint_FeedsEnvelope_ThroughFileConnector()
    {
        var connector = CreateConnector();

        await _input.SubscribeAsync<string>("file-topic", "file-group",
            async envelope =>
            {
                await connector.WriteAsync(envelope, s => Encoding.UTF8.GetBytes(s), CancellationToken.None);
            });

        var env1 = IntegrationEnvelope<string>.Create("order-1", "Svc", "order.placed");
        var env2 = IntegrationEnvelope<string>.Create("order-2", "Svc", "order.shipped");

        await _input.SendAsync(env1);
        await _input.SendAsync(env2);

        var writtenPaths = _fs.Calls
            .Where(c => c.Operation == "WriteAllBytes" && !c.Path!.EndsWith(".meta.json"))
            .Select(c => c.Path)
            .ToList();
        Assert.That(writtenPaths, Has.Count.EqualTo(2));
    }

    private FileConnector CreateConnector() =>
        new(_fs, Options.Create(new FileConnectorOptions
        {
            RootDirectory = "/output",
            CreateDirectoryIfNotExists = true,
        }), NullLogger<FileConnector>.Instance);
}
