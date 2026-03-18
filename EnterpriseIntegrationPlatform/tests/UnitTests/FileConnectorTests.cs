using EnterpriseIntegrationPlatform.Connector.FileSystem;
using EnterpriseIntegrationPlatform.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Text;
using System.Text.Json;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class FileConnectorTests
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    private FileConnector BuildConnector(FileConnectorOptions? options = null) =>
        new FileConnector(
            _fileSystem,
            Options.Create(options ?? new FileConnectorOptions
            {
                RootDirectory = "/data/files",
                FilenamePattern = "{MessageId}-{MessageType}.json",
                CreateDirectoryIfNotExists = true,
                OverwriteExisting = false
            }),
            NullLogger<FileConnector>.Instance);

    private static IntegrationEnvelope<string> BuildEnvelope(string payload = "test") =>
        IntegrationEnvelope<string>.Create(payload, "TestService", "OrderPlaced");

    private static byte[] Utf8Bytes(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public async Task WriteAsync_ValidEnvelope_ReturnsCorrectPath()
    {
        var connector = BuildConnector();
        var envelope = BuildEnvelope();

        var result = await connector.WriteAsync(envelope, Utf8Bytes, CancellationToken.None);

        var expectedFilename = $"{envelope.MessageId}-OrderPlaced.json";
        result.Should().Be(Path.Combine("/data/files", expectedFilename));
    }

    [Fact]
    public async Task WriteAsync_ValidEnvelope_ExpandsFilenamePattern()
    {
        var connector = BuildConnector(new FileConnectorOptions
        {
            RootDirectory = "/data",
            FilenamePattern = "{CorrelationId}-{MessageType}-{Timestamp:yyyyMMddHHmmss}.json"
        });
        var envelope = BuildEnvelope();
        string? capturedPath = null;
        await _fileSystem.WriteAllBytesAsync(
            Arg.Do<string>(p => capturedPath ??= p),
            Arg.Any<byte[]>(),
            Arg.Any<CancellationToken>());

        await connector.WriteAsync(envelope, Utf8Bytes, CancellationToken.None);

        capturedPath.Should().Contain(envelope.CorrelationId.ToString());
        capturedPath.Should().Contain("OrderPlaced");
        capturedPath.Should().Contain(envelope.Timestamp.ToString("yyyyMMddHHmmss"));
    }

    [Fact]
    public async Task WriteAsync_ValidEnvelope_WritesMetadataSidecar()
    {
        var connector = BuildConnector();
        var envelope = BuildEnvelope();
        var writtenPaths = new List<string>();
        await _fileSystem.WriteAllBytesAsync(
            Arg.Do<string>(p => writtenPaths.Add(p)),
            Arg.Any<byte[]>(),
            Arg.Any<CancellationToken>());

        await connector.WriteAsync(envelope, Utf8Bytes, CancellationToken.None);

        writtenPaths.Should().Contain(p => p.EndsWith(".meta.json"));
    }

    [Fact]
    public async Task WriteAsync_FileExistsAndOverwriteFalse_ThrowsInvalidOperationException()
    {
        var opts = new FileConnectorOptions
        {
            RootDirectory = "/data",
            OverwriteExisting = false
        };
        var connector = BuildConnector(opts);
        var envelope = BuildEnvelope();
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);

        var act = async () => await connector.WriteAsync(envelope, Utf8Bytes, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task WriteAsync_FileExistsAndOverwriteTrue_WritesFile()
    {
        var opts = new FileConnectorOptions
        {
            RootDirectory = "/data",
            OverwriteExisting = true
        };
        var connector = BuildConnector(opts);
        var envelope = BuildEnvelope();
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);

        var act = async () => await connector.WriteAsync(envelope, Utf8Bytes, CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _fileSystem.Received().WriteAllBytesAsync(
            Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WriteAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var connector = BuildConnector();

        var act = async () =>
            await connector.WriteAsync<string>(null!, Utf8Bytes, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WriteAsync_CreateDirectoryIfNotExists_True_CreatesDirectory()
    {
        var opts = new FileConnectorOptions
        {
            RootDirectory = "/new/dir",
            CreateDirectoryIfNotExists = true
        };
        var connector = BuildConnector(opts);

        await connector.WriteAsync(BuildEnvelope(), Utf8Bytes, CancellationToken.None);

        _fileSystem.Received(1).CreateDirectory("/new/dir");
    }

    [Fact]
    public async Task ReadAsync_ValidPath_ReturnsBytes()
    {
        var expected = new byte[] { 1, 2, 3 };
        _fileSystem.ReadAllBytesAsync("/data/file.json", Arg.Any<CancellationToken>())
            .Returns(expected);
        var connector = BuildConnector();

        var result = await connector.ReadAsync("/data/file.json", CancellationToken.None);

        result.Should().Equal(expected);
    }

    [Fact]
    public async Task ListFilesAsync_CallsGetFilesWithSearchPattern()
    {
        var connector = BuildConnector();
        _fileSystem.GetFiles("/data/files", "*.json")
            .Returns(new[] { "/data/files/a.json", "/data/files/b.json" });

        var result = await connector.ListFilesAsync(null, "*.json", CancellationToken.None);

        result.Should().BeEquivalentTo("/data/files/a.json", "/data/files/b.json");
        _fileSystem.Received(1).GetFiles("/data/files", "*.json");
    }

    [Fact]
    public async Task WriteAsync_EmptyRootDirectory_ThrowsArgumentException()
    {
        var opts = new FileConnectorOptions { RootDirectory = "" };
        var connector = BuildConnector(opts);

        var act = async () => await connector.WriteAsync(BuildEnvelope(), Utf8Bytes, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
