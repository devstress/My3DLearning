// ============================================================================
// Tutorial 37 – File Connector (Lab)
// ============================================================================
// EIP Pattern: Connector.
// E2E: Wire real FileConnector with NSubstitute IFileSystem and
// MockEndpoint to simulate envelope-driven file I/O.
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
public sealed class Lab
{
    private MockEndpoint _input = null!;
    private IFileSystem _fs = null!;

    [SetUp]
    public void SetUp()
    {
        _input = new MockEndpoint("file-in");
        _fs = Substitute.For<IFileSystem>();
    }

    [TearDown]
    public async Task TearDown() => await _input.DisposeAsync();

    [Test]
    public async Task Write_CreatesDataFile_AndMetadataSidecar()
    {
        var connector = CreateConnector();
        var envelope = IntegrationEnvelope<string>.Create("payload", "Svc", "order.placed");

        await connector.WriteAsync(envelope, s => Encoding.UTF8.GetBytes(s), CancellationToken.None);

        _fs.Received(1).CreateDirectory(Arg.Any<string>());
        await _fs.Received(2).WriteAllBytesAsync(
            Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Write_ExpandsFilenamePattern_FromEnvelope()
    {
        string? capturedPath = null;
        _fs.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci =>
            {
                var path = ci.ArgAt<string>(0);
                if (!path.EndsWith(".meta.json"))
                    capturedPath = path;
            });

        var connector = CreateConnector();
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "invoice.created");

        await connector.WriteAsync(envelope, s => Encoding.UTF8.GetBytes(s), CancellationToken.None);

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

        _fs.Received(1).CreateDirectory("/output");
    }

    [Test]
    public async Task Write_Throws_WhenFileExists_AndOverwriteDisabled()
    {
        _fs.FileExists(Arg.Any<string>()).Returns(true);

        var connector = new FileConnector(_fs,
            Options.Create(new FileConnectorOptions
            {
                RootDirectory = "/output",
                CreateDirectoryIfNotExists = true,
                OverwriteExisting = false,
            }),
            NullLogger<FileConnector>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "test.event");

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await connector.WriteAsync(envelope, s => Encoding.UTF8.GetBytes(s), CancellationToken.None));
    }

    [Test]
    public async Task Read_ReturnsFileContent()
    {
        var expected = Encoding.UTF8.GetBytes("file-content");
        _fs.ReadAllBytesAsync("/output/test.json", Arg.Any<CancellationToken>())
            .Returns(expected);

        var connector = CreateConnector();
        var result = await connector.ReadAsync("/output/test.json", CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task ListFiles_ReturnsMatchingPaths()
    {
        _fs.GetFiles(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new[] { "/output/a.json", "/output/b.json" });

        var connector = CreateConnector();
        var files = await connector.ListFilesAsync(null, "*.json", CancellationToken.None);

        Assert.That(files, Has.Count.EqualTo(2));
        Assert.That(files, Does.Contain("/output/a.json"));
    }

    [Test]
    public async Task E2E_MockEndpoint_FeedsEnvelope_ThroughFileConnector()
    {
        var writtenPaths = new List<string>();
        _fs.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci =>
            {
                var path = ci.ArgAt<string>(0);
                if (!path.EndsWith(".meta.json"))
                    writtenPaths.Add(path);
            });

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

        Assert.That(writtenPaths, Has.Count.EqualTo(2));
    }

    private FileConnector CreateConnector() =>
        new(_fs, Options.Create(new FileConnectorOptions
        {
            RootDirectory = "/output",
            CreateDirectoryIfNotExists = true,
        }), NullLogger<FileConnector>.Instance);
}
