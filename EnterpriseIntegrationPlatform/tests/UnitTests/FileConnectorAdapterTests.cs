using EnterpriseIntegrationPlatform.Connector.FileSystem;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class FileConnectorAdapterTests
{
    private sealed record TestPayload(string Content);

    private IFileConnector _fileConnector = null!;
    private IFileSystem _fileSystem = null!;
    private FileConnectorAdapter _adapter = null!;

    private static IntegrationEnvelope<TestPayload> BuildEnvelope() =>
        IntegrationEnvelope<TestPayload>.Create(new TestPayload("data"), "UnitTest", "TestEvent");

    [SetUp]
    public void SetUp()
    {
        _fileConnector = Substitute.For<IFileConnector>();
        _fileSystem = Substitute.For<IFileSystem>();
        _adapter = new FileConnectorAdapter(
            "file-test",
            _fileConnector,
            Options.Create(new FileConnectorOptions { RootDirectory = "/data/out" }),
            _fileSystem,
            NullLogger<FileConnectorAdapter>.Instance);
    }

    [Test]
    public void Name_ReturnsConfiguredName()
    {
        Assert.That(_adapter.Name, Is.EqualTo("file-test"));
    }

    [Test]
    public void ConnectorType_IsFile()
    {
        Assert.That(_adapter.ConnectorType, Is.EqualTo(ConnectorType.File));
    }

    [Test]
    public async Task SendAsync_Success_ReturnsOkResult()
    {
        _fileConnector.WriteAsync(
            Arg.Any<IntegrationEnvelope<TestPayload>>(),
            Arg.Any<Func<TestPayload, byte[]>>(),
            Arg.Any<CancellationToken>())
            .Returns("/data/out/msg-123.json");

        var options = new ConnectorSendOptions();
        var result = await _adapter.SendAsync(BuildEnvelope(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConnectorName, Is.EqualTo("file-test"));
        Assert.That(result.StatusMessage, Does.Contain("/data/out/msg-123.json"));
    }

    [Test]
    public async Task SendAsync_Exception_ReturnsFailResult()
    {
        _fileConnector.WriteAsync(
            Arg.Any<IntegrationEnvelope<TestPayload>>(),
            Arg.Any<Func<TestPayload, byte[]>>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("Disk full"));

        var options = new ConnectorSendOptions();
        var result = await _adapter.SendAsync(BuildEnvelope(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Disk full"));
    }

    [Test]
    public async Task TestConnectionAsync_DirectoryConfigured_ReturnsTrue()
    {
        var healthy = await _adapter.TestConnectionAsync();

        Assert.That(healthy, Is.True);
        _fileSystem.Received(1).CreateDirectory("/data/out");
    }

    [Test]
    public async Task TestConnectionAsync_NoRootDirectory_ReturnsFalse()
    {
        var adapter = new FileConnectorAdapter(
            "file-no-root",
            _fileConnector,
            Options.Create(new FileConnectorOptions { RootDirectory = "" }),
            _fileSystem,
            NullLogger<FileConnectorAdapter>.Instance);

        var healthy = await adapter.TestConnectionAsync();
        Assert.That(healthy, Is.False);
    }

    [Test]
    public async Task TestConnectionAsync_CreateDirectoryThrows_ReturnsFalse()
    {
        _fileSystem.When(x => x.CreateDirectory(Arg.Any<string>()))
            .Do(_ => throw new UnauthorizedAccessException("Permission denied"));

        var healthy = await _adapter.TestConnectionAsync();
        Assert.That(healthy, Is.False);
    }

    [Test]
    public void Constructor_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FileConnectorAdapter(
                null!,
                _fileConnector,
                Options.Create(new FileConnectorOptions()),
                _fileSystem,
                NullLogger<FileConnectorAdapter>.Instance));
    }

    [Test]
    public void Constructor_NullConnector_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FileConnectorAdapter(
                "test",
                null!,
                Options.Create(new FileConnectorOptions()),
                _fileSystem,
                NullLogger<FileConnectorAdapter>.Instance));
    }

    [Test]
    public void SendAsync_NullEnvelope_Throws()
    {
        var options = new ConnectorSendOptions();
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _adapter.SendAsync<TestPayload>(null!, options));
    }

    [Test]
    public void SendAsync_NullOptions_Throws()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _adapter.SendAsync(BuildEnvelope(), null!));
    }
}
