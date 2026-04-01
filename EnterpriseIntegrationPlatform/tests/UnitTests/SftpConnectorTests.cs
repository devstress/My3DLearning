using EnterpriseIntegrationPlatform.Connector.Sftp;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class SftpConnectorTests
{
    private readonly ISftpClient _sftpClient = Substitute.For<ISftpClient>();

    private SftpConnector BuildConnector(string rootPath = "/uploads") =>
        new SftpConnector(
            _sftpClient,
            Options.Create(new SftpConnectorOptions { RootPath = rootPath }),
            NullLogger<SftpConnector>.Instance);

    private static IntegrationEnvelope<string> BuildEnvelope(string payload = "test-data") =>
        IntegrationEnvelope<string>.Create(payload, "TestService", "TestEvent");

    private static byte[] Utf8Bytes(string s) => Encoding.UTF8.GetBytes(s);

    [Test]
    public async Task UploadAsync_ValidEnvelope_UploadsToCorrectRemotePath()
    {
        var connector = BuildConnector("/files");
        var envelope = BuildEnvelope("payload");
        string? capturedPath = null;
        _sftpClient
            .When(c => c.UploadFile(Arg.Any<Stream>(), Arg.Any<string>()))
            .Do(ci => capturedPath ??= (string)ci[1]);

        await connector.UploadAsync(envelope, "data.json", Utf8Bytes, CancellationToken.None);

        Assert.That(capturedPath, Is.EqualTo("/files/data.json"));
    }

    [Test]
    public async Task UploadAsync_ValidEnvelope_WritesMetadataFile()
    {
        var connector = BuildConnector("/files");
        var envelope = BuildEnvelope();
        var uploadedPaths = new List<string>();
        _sftpClient
            .When(c => c.UploadFile(Arg.Any<Stream>(), Arg.Any<string>()))
            .Do(ci => uploadedPaths.Add((string)ci[1]));

        await connector.UploadAsync(envelope, "data.json", Utf8Bytes, CancellationToken.None);

        Assert.That(uploadedPaths, Does.Contain("/files/data.json.meta"));
    }

    [Test]
    public async Task DownloadAsync_ValidPath_ReturnsFileBytes()
    {
        var connector = BuildConnector();
        var expected = Encoding.UTF8.GetBytes("file-content");
        _sftpClient.DownloadFile("/uploads/file.json")
            .Returns(new MemoryStream(expected));

        var result = await connector.DownloadAsync("/uploads/file.json", CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task ListFilesAsync_ValidPath_ReturnsFiles()
    {
        var connector = BuildConnector();
        _sftpClient.ListFiles("/uploads").Returns(new[] { "/uploads/a.json", "/uploads/b.json" });

        var result = await connector.ListFilesAsync("/uploads", CancellationToken.None);

        Assert.That(result, Is.EquivalentTo(new[] { "/uploads/a.json", "/uploads/b.json" }));
    }

    [Test]
    public async Task UploadAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var connector = BuildConnector();

        var act = async () =>
            await connector.UploadAsync<string>(null!, "file.json", Utf8Bytes, CancellationToken.None);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }

    [Test]
    public async Task UploadAsync_CallsConnect_BeforeUpload()
    {
        var connector = BuildConnector();
        var envelope = BuildEnvelope();
        var order = new List<string>();
        _sftpClient.When(c => c.Connect()).Do(_ => order.Add("Connect"));
        _sftpClient.When(c => c.UploadFile(Arg.Any<Stream>(), Arg.Any<string>()))
            .Do(_ => order.Add("UploadFile"));

        await connector.UploadAsync(envelope, "data.json", Utf8Bytes, CancellationToken.None);

        Assert.That(order.IndexOf("Connect"), Is.LessThan(order.IndexOf("UploadFile")));
    }

    [Test]
    public async Task UploadAsync_CallsDisconnect_InFinally()
    {
        var connector = BuildConnector();
        var envelope = BuildEnvelope();
        _sftpClient
            .When(c => c.UploadFile(Arg.Any<Stream>(), Arg.Is<string>(p => !p.EndsWith(".meta"))))
            .Do(_ => throw new InvalidOperationException("simulated SFTP error"));

        var act = async () =>
            await connector.UploadAsync(envelope, "data.json", Utf8Bytes, CancellationToken.None);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await act());
        _sftpClient.Received(1).Disconnect();
    }

    [Test]
    public async Task DownloadAsync_CallsConnect_BeforeDownload()
    {
        var connector = BuildConnector();
        var order = new List<string>();
        _sftpClient.When(c => c.Connect()).Do(_ => order.Add("Connect"));
        _sftpClient.DownloadFile(Arg.Any<string>())
            .Returns(_ => { order.Add("DownloadFile"); return new MemoryStream(); });

        await connector.DownloadAsync("/uploads/f.json", CancellationToken.None);

        Assert.That(order.IndexOf("Connect"), Is.LessThan(order.IndexOf("DownloadFile")));
    }

    [Test]
    public async Task DownloadAsync_CallsDisconnect_AfterDownload()
    {
        var connector = BuildConnector();
        _sftpClient.DownloadFile(Arg.Any<string>()).Returns(new MemoryStream());

        await connector.DownloadAsync("/uploads/f.json", CancellationToken.None);

        _sftpClient.Received(1).Disconnect();
    }

    [Test]
    public async Task DownloadAsync_ReturnsCorrectBytes()
    {
        var connector = BuildConnector();
        var expected = new byte[] { 0x42, 0x43, 0x44 };
        _sftpClient.DownloadFile("/uploads/binary.bin").Returns(new MemoryStream(expected));

        var result = await connector.DownloadAsync("/uploads/binary.bin", CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }
}
