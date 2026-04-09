using EnterpriseIntegrationPlatform.Connector.Sftp;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class SftpConnectorAdapterTests
{
    private sealed record TestPayload(string Data);

    private ISftpConnector _sftpConnector = null!;
    private ISftpClient _sftpClient = null!;
    private SftpConnectorAdapter _adapter = null!;

    private static IntegrationEnvelope<TestPayload> BuildEnvelope() =>
        IntegrationEnvelope<TestPayload>.Create(new TestPayload("data"), "UnitTest", "TestEvent");

    [SetUp]
    public void SetUp()
    {
        _sftpConnector = Substitute.For<ISftpConnector>();
        _sftpClient = Substitute.For<ISftpClient>();
        _adapter = new SftpConnectorAdapter(
            "sftp-test",
            _sftpConnector,
            _sftpClient,
            NullLogger<SftpConnectorAdapter>.Instance);
    }

    [Test]
    public void Name_ReturnsConfiguredName()
    {
        Assert.That(_adapter.Name, Is.EqualTo("sftp-test"));
    }

    [Test]
    public void ConnectorType_IsSftp()
    {
        Assert.That(_adapter.ConnectorType, Is.EqualTo(ConnectorType.Sftp));
    }

    [Test]
    public async Task SendAsync_Success_ReturnsOkResult()
    {
        _sftpConnector.UploadAsync(
            Arg.Any<IntegrationEnvelope<TestPayload>>(),
            Arg.Any<string>(),
            Arg.Any<Func<TestPayload, byte[]>>(),
            Arg.Any<CancellationToken>())
            .Returns("/remote/path/file.json");

        var options = new ConnectorSendOptions { Destination = "custom-file.json" };
        var result = await _adapter.SendAsync(BuildEnvelope(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConnectorName, Is.EqualTo("sftp-test"));
        Assert.That(result.StatusMessage, Does.Contain("/remote/path/file.json"));
    }

    [Test]
    public async Task SendAsync_NoDestination_GeneratesDefaultFilename()
    {
        _sftpConnector.UploadAsync(
            Arg.Any<IntegrationEnvelope<TestPayload>>(),
            Arg.Any<string>(),
            Arg.Any<Func<TestPayload, byte[]>>(),
            Arg.Any<CancellationToken>())
            .Returns("/remote/default.json");

        var options = new ConnectorSendOptions(); // no Destination
        var result = await _adapter.SendAsync(BuildEnvelope(), options);

        Assert.That(result.Success, Is.True);
        // The adapter generates a filename from MessageId-MessageType
        await _sftpConnector.Received(1).UploadAsync(
            Arg.Any<IntegrationEnvelope<TestPayload>>(),
            Arg.Is<string>(f => f.Contains("TestEvent")),
            Arg.Any<Func<TestPayload, byte[]>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendAsync_Exception_ReturnsFailResult()
    {
        _sftpConnector.UploadAsync(
            Arg.Any<IntegrationEnvelope<TestPayload>>(),
            Arg.Any<string>(),
            Arg.Any<Func<TestPayload, byte[]>>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("SFTP connection lost"));

        var options = new ConnectorSendOptions { Destination = "file.json" };
        var result = await _adapter.SendAsync(BuildEnvelope(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("SFTP connection lost"));
    }

    [Test]
    public async Task TestConnectionAsync_Connected_ReturnsTrue()
    {
        _sftpClient.IsConnected.Returns(true);
        var healthy = await _adapter.TestConnectionAsync();

        Assert.That(healthy, Is.True);
        _sftpClient.Received(1).Connect();
        _sftpClient.Received(1).Disconnect();
    }

    [Test]
    public async Task TestConnectionAsync_NotConnected_ReturnsFalse()
    {
        _sftpClient.IsConnected.Returns(false);
        var healthy = await _adapter.TestConnectionAsync();

        Assert.That(healthy, Is.False);
        _sftpClient.Received(1).Disconnect();
    }

    [Test]
    public async Task TestConnectionAsync_ConnectThrows_ReturnsFalse()
    {
        _sftpClient.When(x => x.Connect()).Do(_ => throw new Exception("Connection refused"));
        var healthy = await _adapter.TestConnectionAsync();
        Assert.That(healthy, Is.False);
    }

    [Test]
    public void Constructor_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SftpConnectorAdapter(
                null!,
                _sftpConnector,
                _sftpClient,
                NullLogger<SftpConnectorAdapter>.Instance));
    }

    [Test]
    public void Constructor_NullConnector_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SftpConnectorAdapter(
                "test",
                null!,
                _sftpClient,
                NullLogger<SftpConnectorAdapter>.Instance));
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
