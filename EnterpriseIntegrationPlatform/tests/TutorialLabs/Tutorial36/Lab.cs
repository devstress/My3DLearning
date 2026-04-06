// ============================================================================
// Tutorial 36 – Connector.Email (Lab)
// ============================================================================
// This lab exercises EmailConnectorOptions, ISmtpClientWrapper, EmailConnector,
// and IEmailConnector to learn the Email connector subsystem.
// ============================================================================

using EnterpriseIntegrationPlatform.Connector.Email;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial36;

[TestFixture]
public sealed class Lab
{
    // ── EmailConnectorOptions Defaults ──────────────────────────────────────

    [Test]
    public void EmailConnectorOptions_Defaults()
    {
        var opts = new EmailConnectorOptions();

        Assert.That(opts.SmtpHost, Is.EqualTo(string.Empty));
        Assert.That(opts.SmtpPort, Is.EqualTo(587));
        Assert.That(opts.UseTls, Is.True);
        Assert.That(opts.Username, Is.EqualTo(string.Empty));
        Assert.That(opts.Password, Is.EqualTo(string.Empty));
        Assert.That(opts.DefaultFrom, Is.EqualTo(string.Empty));
        Assert.That(opts.DefaultSubjectTemplate, Is.EqualTo("{MessageType} notification"));
    }

    // ── EmailConnectorOptions Custom Values ─────────────────────────────────

    [Test]
    public void EmailConnectorOptions_CustomValues()
    {
        var opts = new EmailConnectorOptions
        {
            SmtpHost = "mail.example.com",
            SmtpPort = 465,
            UseTls = false,
            Username = "user@example.com",
            Password = "secret",
            DefaultFrom = "noreply@example.com",
            DefaultSubjectTemplate = "Alert: {MessageType}",
        };

        Assert.That(opts.SmtpHost, Is.EqualTo("mail.example.com"));
        Assert.That(opts.SmtpPort, Is.EqualTo(465));
        Assert.That(opts.UseTls, Is.False);
        Assert.That(opts.Username, Is.EqualTo("user@example.com"));
        Assert.That(opts.Password, Is.EqualTo("secret"));
        Assert.That(opts.DefaultFrom, Is.EqualTo("noreply@example.com"));
        Assert.That(opts.DefaultSubjectTemplate, Is.EqualTo("Alert: {MessageType}"));
    }

    // ── ISmtpClientWrapper Interface Shape (Reflection) ─────────────────────

    [Test]
    public void ISmtpClientWrapper_InterfaceShape_HasExpectedMembers()
    {
        var type = typeof(ISmtpClientWrapper);

        Assert.That(type.GetMethod("ConnectAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("AuthenticateAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("SendAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("DisconnectAsync"), Is.Not.Null);
        Assert.That(type.GetProperty("IsConnected"), Is.Not.Null);
    }

    // ── EmailConnector Sends via Mocked SMTP ────────────────────────────────

    [Test]
    public async Task EmailConnector_Send_DelegatesToSmtpWrapper()
    {
        var smtpClient = Substitute.For<ISmtpClientWrapper>();
        smtpClient.IsConnected.Returns(false);

        var opts = Options.Create(new EmailConnectorOptions
        {
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            UseTls = true,
            Username = "user",
            Password = "pass",
            DefaultFrom = "test@test.com",
        });

        var connector = new EmailConnector(smtpClient, opts, NullLogger<EmailConnector>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("Hello", "Svc", "order.placed");

        await connector.SendAsync(
            envelope, "dest@test.com", "Test Subject", p => p, CancellationToken.None);

        await smtpClient.Received(1).ConnectAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await smtpClient.Received(1).SendAsync(
            Arg.Any<MimeKit.MimeMessage>(), Arg.Any<CancellationToken>());
        await smtpClient.Received(1).DisconnectAsync(
            Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    // ── EmailConnector Constructor Requires All Dependencies ────────────────

    [Test]
    public void EmailConnector_Constructor_AcceptsAllDependencies()
    {
        var smtpClient = Substitute.For<ISmtpClientWrapper>();
        var opts = Options.Create(new EmailConnectorOptions());
        var logger = NullLogger<EmailConnector>.Instance;

        var connector = new EmailConnector(smtpClient, opts, logger);

        Assert.That(connector, Is.Not.Null);
    }

    // ── IEmailConnector Interface Has SendAsync Methods (Reflection) ────────

    [Test]
    public void IEmailConnector_InterfaceShape_HasSendAsyncMethods()
    {
        var type = typeof(IEmailConnector);
        var methods = type.GetMethods().Where(m => m.Name == "SendAsync").ToArray();

        Assert.That(methods.Length, Is.GreaterThanOrEqualTo(2),
            "IEmailConnector should have at least two SendAsync overloads");
    }

    // ── DefaultSubjectTemplate Contains MessageType Placeholder ─────────────

    [Test]
    public void DefaultSubjectTemplate_ContainsMessageTypePlaceholder()
    {
        var opts = new EmailConnectorOptions();

        Assert.That(opts.DefaultSubjectTemplate, Does.Contain("{MessageType}"));
    }
}
