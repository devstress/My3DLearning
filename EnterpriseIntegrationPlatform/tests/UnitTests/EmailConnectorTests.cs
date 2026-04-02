using EnterpriseIntegrationPlatform.Connector.Email;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class EmailConnectorTests
{
    private ISmtpClientWrapper _smtpClient = null!;

    [SetUp]
    public void SetUp()
    {
        _smtpClient = Substitute.For<ISmtpClientWrapper>();
    }

    private EmailConnector BuildConnector(EmailConnectorOptions? options = null) =>
        new EmailConnector(
            _smtpClient,
            Options.Create(options ?? new EmailConnectorOptions
            {
                SmtpHost = "smtp.example.com",
                SmtpPort = 587,
                UseTls = true,
                Username = "user",
                Password = "pass",
                DefaultFrom = "sender@example.com",
                DefaultSubjectTemplate = "{MessageType} notification"
            }),
            NullLogger<EmailConnector>.Instance);

    private static IntegrationEnvelope<string> BuildEnvelope(string payload = "test") =>
        IntegrationEnvelope<string>.Create(payload, "TestService", "OrderPlaced");

    [Test]
    public async Task SendAsync_ValidEnvelope_SendsToCorrectToAddress()
    {
        var connector = BuildConnector();
        MimeMessage? captured = null;
        await _smtpClient.SendAsync(Arg.Do<MimeMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await connector.SendAsync(BuildEnvelope(), "recipient@example.com", null, p => p, CancellationToken.None);

        Assert.That(captured!.To.Mailboxes.Count(m => m.Address == "recipient@example.com"), Is.EqualTo(1));
    }

    [Test]
    public async Task SendAsync_ValidEnvelope_SendsFromDefaultFrom()
    {
        var connector = BuildConnector();
        MimeMessage? captured = null;
        await _smtpClient.SendAsync(Arg.Do<MimeMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await connector.SendAsync(BuildEnvelope(), "recipient@example.com", null, p => p, CancellationToken.None);

        Assert.That(captured!.From.Mailboxes.Count(m => m.Address == "sender@example.com"), Is.EqualTo(1));
    }

    [Test]
    public async Task SendAsync_NoSubject_UsesDefaultSubjectTemplate()
    {
        var connector = BuildConnector();
        MimeMessage? captured = null;
        await _smtpClient.SendAsync(Arg.Do<MimeMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await connector.SendAsync(BuildEnvelope(), "recipient@example.com", null, p => p, CancellationToken.None);

        Assert.That(captured!.Subject, Is.EqualTo("OrderPlaced notification"));
    }

    [Test]
    public async Task SendAsync_CustomSubject_UsesCustomSubject()
    {
        var connector = BuildConnector();
        MimeMessage? captured = null;
        await _smtpClient.SendAsync(Arg.Do<MimeMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await connector.SendAsync(BuildEnvelope(), "recipient@example.com", "My Custom Subject", p => p, CancellationToken.None);

        Assert.That(captured!.Subject, Is.EqualTo("My Custom Subject"));
    }

    [Test]
    public async Task SendAsync_ValidEnvelope_BodyBuiltFromPayload()
    {
        var connector = BuildConnector();
        MimeMessage? captured = null;
        await _smtpClient.SendAsync(Arg.Do<MimeMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await connector.SendAsync(BuildEnvelope("payload-value"), "r@example.com", null,
            p => $"Body: {p}", CancellationToken.None);

        Assert.That(captured!.TextBody, Is.EqualTo("Body: payload-value"));
    }

    [Test]
    public async Task SendAsync_ValidEnvelope_IncludesCorrelationHeader()
    {
        var connector = BuildConnector();
        var envelope = BuildEnvelope();
        MimeMessage? captured = null;
        await _smtpClient.SendAsync(Arg.Do<MimeMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await connector.SendAsync(envelope, "r@example.com", null, p => p, CancellationToken.None);

        Assert.That(captured!.Headers["X-Correlation-Id"], Is.EqualTo(envelope.CorrelationId.ToString()));
    }

    [Test]
    public async Task SendAsync_ValidEnvelope_ConnectsBeforeSend()
    {
        var connector = BuildConnector();
        var order = new List<string>();
        _smtpClient.When(c => c.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()))
            .Do(_ => order.Add("Connect"));
        _smtpClient.When(c => c.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>()))
            .Do(_ => order.Add("Send"));

        await connector.SendAsync(BuildEnvelope(), "r@example.com", null, p => p, CancellationToken.None);

        Assert.That(order.IndexOf("Connect"), Is.LessThan(order.IndexOf("Send")));
    }

    [Test]
    public async Task SendAsync_ValidEnvelope_DisconnectsAfterSend()
    {
        var connector = BuildConnector();
        _smtpClient.When(c => c.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("simulated SMTP error"));

        var act = async () =>
            await connector.SendAsync(BuildEnvelope(), "r@example.com", null, p => p, CancellationToken.None);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await act());
        await _smtpClient.Received(1).DisconnectAsync(true, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var connector = BuildConnector();

        var act = async () =>
            await connector.SendAsync<string>(null!, "r@example.com", null, p => p, CancellationToken.None);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }

    [Test]
    public async Task SendAsync_EmptyToAddresses_ThrowsArgumentException()
    {
        var connector = BuildConnector();

        var act = async () =>
            await connector.SendAsync(BuildEnvelope(), Array.Empty<string>(), null, p => p, CancellationToken.None);

        Assert.ThrowsAsync<ArgumentException>(async () => await act());
    }
}
