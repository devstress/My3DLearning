using EnterpriseIntegrationPlatform.Connector.Email;
using EnterpriseIntegrationPlatform.Connectors;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class EmailConnectorAdapterTests
{
    private sealed record TestPayload(string Body);

    private IEmailConnector _emailConnector = null!;
    private ISmtpClientWrapper _smtpClient = null!;
    private EmailConnectorAdapter _adapter = null!;

    private static IntegrationEnvelope<TestPayload> BuildEnvelope() =>
        IntegrationEnvelope<TestPayload>.Create(new TestPayload("hello"), "UnitTest", "TestEvent");

    [SetUp]
    public void SetUp()
    {
        _emailConnector = Substitute.For<IEmailConnector>();
        _smtpClient = Substitute.For<ISmtpClientWrapper>();
        _adapter = new EmailConnectorAdapter(
            "email-test",
            _emailConnector,
            _smtpClient,
            NullLogger<EmailConnectorAdapter>.Instance);
    }

    [Test]
    public void Name_ReturnsConfiguredName()
    {
        Assert.That(_adapter.Name, Is.EqualTo("email-test"));
    }

    [Test]
    public void ConnectorType_IsEmail()
    {
        Assert.That(_adapter.ConnectorType, Is.EqualTo(ConnectorType.Email));
    }

    [Test]
    public async Task SendAsync_Success_ReturnsOkResult()
    {
        var options = new ConnectorSendOptions { Destination = "user@example.com" };
        var result = await _adapter.SendAsync(BuildEnvelope(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConnectorName, Is.EqualTo("email-test"));
        Assert.That(result.StatusMessage, Does.Contain("user@example.com"));
    }

    [Test]
    public async Task SendAsync_WithSubjectProperty_PassesSubject()
    {
        var options = new ConnectorSendOptions
        {
            Destination = "admin@test.com",
            Properties = new Dictionary<string, string> { ["Subject"] = "Alert" },
        };
        await _adapter.SendAsync(BuildEnvelope(), options);

        await _emailConnector.Received(1).SendAsync(
            Arg.Any<IntegrationEnvelope<TestPayload>>(),
            "admin@test.com",
            "Alert",
            Arg.Any<Func<TestPayload, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void SendAsync_NoDestination_ThrowsInvalidOperationException()
    {
        var options = new ConnectorSendOptions(); // no Destination
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _adapter.SendAsync(BuildEnvelope(), options));
    }

    [Test]
    public async Task SendAsync_Exception_ReturnsFailResult()
    {
        _emailConnector.SendAsync(
            Arg.Any<IntegrationEnvelope<TestPayload>>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<Func<TestPayload, string>>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP error"));

        var options = new ConnectorSendOptions { Destination = "fail@test.com" };
        var result = await _adapter.SendAsync(BuildEnvelope(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("SMTP error"));
    }

    [Test]
    public async Task TestConnectionAsync_Connected_ReturnsTrue()
    {
        _smtpClient.IsConnected.Returns(true);
        var healthy = await _adapter.TestConnectionAsync();
        Assert.That(healthy, Is.True);
    }

    [Test]
    public async Task TestConnectionAsync_NotConnected_StillReturnsTrue()
    {
        // EmailConnectorAdapter returns true even when not connected (optimistic health)
        _smtpClient.IsConnected.Returns(false);
        var healthy = await _adapter.TestConnectionAsync();
        Assert.That(healthy, Is.True);
    }

    [Test]
    public void Constructor_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new EmailConnectorAdapter(
                null!,
                _emailConnector,
                _smtpClient,
                NullLogger<EmailConnectorAdapter>.Instance));
    }

    [Test]
    public void Constructor_NullConnector_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new EmailConnectorAdapter(
                "test",
                null!,
                _smtpClient,
                NullLogger<EmailConnectorAdapter>.Instance));
    }

    [Test]
    public void SendAsync_NullEnvelope_Throws()
    {
        var options = new ConnectorSendOptions { Destination = "x@test.com" };
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _adapter.SendAsync<TestPayload>(null!, options));
    }
}
