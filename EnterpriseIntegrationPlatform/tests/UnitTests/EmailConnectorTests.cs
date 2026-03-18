using EnterpriseIntegrationPlatform.Connector.Email;
using EnterpriseIntegrationPlatform.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using NSubstitute;
using Xunit;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

public class EmailConnectorTests
{
    private readonly ISmtpClientWrapper _smtpClient = Substitute.For<ISmtpClientWrapper>();

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

    [Fact]
    public async Task SendAsync_ValidEnvelope_SendsToCorrectToAddress()
    {
        var connector = BuildConnector();
        MimeMessage? captured = null;
        await _smtpClient.SendAsync(Arg.Do<MimeMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await connector.SendAsync(BuildEnvelope(), "recipient@example.com", null, p => p, CancellationToken.None);

        captured!.To.Mailboxes.Should().ContainSingle(m => m.Address == "recipient@example.com");
    }

    [Fact]
    public async Task SendAsync_ValidEnvelope_SendsFromDefaultFrom()
    {
        var connector = BuildConnector();
        MimeMessage? captured = null;
        await _smtpClient.SendAsync(Arg.Do<MimeMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await connector.SendAsync(BuildEnvelope(), "recipient@example.com", null, p => p, CancellationToken.None);

        captured!.From.Mailboxes.Should().ContainSingle(m => m.Address == "sender@example.com");
    }

    [Fact]
    public async Task SendAsync_NoSubject_UsesDefaultSubjectTemplate()
    {
        var connector = BuildConnector();
        MimeMessage? captured = null;
        await _smtpClient.SendAsync(Arg.Do<MimeMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await connector.SendAsync(BuildEnvelope(), "recipient@example.com", null, p => p, CancellationToken.None);

        captured!.Subject.Should().Be("OrderPlaced notification");
    }

    [Fact]
    public async Task SendAsync_CustomSubject_UsesCustomSubject()
    {
        var connector = BuildConnector();
        MimeMessage? captured = null;
        await _smtpClient.SendAsync(Arg.Do<MimeMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await connector.SendAsync(BuildEnvelope(), "recipient@example.com", "My Custom Subject", p => p, CancellationToken.None);

        captured!.Subject.Should().Be("My Custom Subject");
    }

    [Fact]
    public async Task SendAsync_ValidEnvelope_BodyBuiltFromPayload()
    {
        var connector = BuildConnector();
        MimeMessage? captured = null;
        await _smtpClient.SendAsync(Arg.Do<MimeMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await connector.SendAsync(BuildEnvelope("payload-value"), "r@example.com", null,
            p => $"Body: {p}", CancellationToken.None);

        captured!.TextBody.Should().Be("Body: payload-value");
    }

    [Fact]
    public async Task SendAsync_ValidEnvelope_IncludesCorrelationHeader()
    {
        var connector = BuildConnector();
        var envelope = BuildEnvelope();
        MimeMessage? captured = null;
        await _smtpClient.SendAsync(Arg.Do<MimeMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await connector.SendAsync(envelope, "r@example.com", null, p => p, CancellationToken.None);

        captured!.Headers["X-Correlation-Id"].Should().Be(envelope.CorrelationId.ToString());
    }

    [Fact]
    public async Task SendAsync_ValidEnvelope_ConnectsBeforeSend()
    {
        var connector = BuildConnector();
        var order = new List<string>();
        _smtpClient.When(c => c.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()))
            .Do(_ => order.Add("Connect"));
        _smtpClient.When(c => c.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>()))
            .Do(_ => order.Add("Send"));

        await connector.SendAsync(BuildEnvelope(), "r@example.com", null, p => p, CancellationToken.None);

        order.IndexOf("Connect").Should().BeLessThan(order.IndexOf("Send"));
    }

    [Fact]
    public async Task SendAsync_ValidEnvelope_DisconnectsAfterSend()
    {
        var connector = BuildConnector();
        _smtpClient.When(c => c.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("simulated SMTP error"));

        var act = async () =>
            await connector.SendAsync(BuildEnvelope(), "r@example.com", null, p => p, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _smtpClient.Received(1).DisconnectAsync(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        var connector = BuildConnector();

        var act = async () =>
            await connector.SendAsync<string>(null!, "r@example.com", null, p => p, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_EmptyToAddresses_ThrowsArgumentException()
    {
        var connector = BuildConnector();

        var act = async () =>
            await connector.SendAsync(BuildEnvelope(), Array.Empty<string>(), null, p => p, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
