// ============================================================================
// Tutorial 36 – Connector.Email (Exam)
// ============================================================================
// Coding challenges: full send lifecycle, multi-recipient email,
// and custom subject template.
// ============================================================================

using EnterpriseIntegrationPlatform.Connector.Email;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial36;

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Full Send Lifecycle ─────────────────────────────────────

    [Test]
    public async Task Challenge1_FullSendLifecycle_ConnectAuthSendDisconnect()
    {
        var smtpClient = Substitute.For<ISmtpClientWrapper>();
        smtpClient.IsConnected.Returns(false);

        var opts = Options.Create(new EmailConnectorOptions
        {
            SmtpHost = "smtp.lifecycle.com",
            SmtpPort = 587,
            UseTls = true,
            Username = "admin",
            Password = "s3cret",
            DefaultFrom = "system@lifecycle.com",
        });

        var connector = new EmailConnector(smtpClient, opts, NullLogger<EmailConnector>.Instance);
        var envelope = IntegrationEnvelope<string>.Create("Order confirmed", "OrderSvc", "order.confirmed");

        await connector.SendAsync(
            envelope, "customer@example.com", "Order Update", p => p, CancellationToken.None);

        Received.InOrder(() =>
        {
            smtpClient.ConnectAsync(
                "smtp.lifecycle.com", 587, true, Arg.Any<CancellationToken>());
            smtpClient.AuthenticateAsync(
                "admin", "s3cret", Arg.Any<CancellationToken>());
            smtpClient.SendAsync(
                Arg.Any<MimeKit.MimeMessage>(), Arg.Any<CancellationToken>());
            smtpClient.DisconnectAsync(
                true, Arg.Any<CancellationToken>());
        });
    }

    // ── Challenge 2: Multi-Recipient Email ──────────────────────────────────

    [Test]
    public async Task Challenge2_MultiRecipientEmail()
    {
        var smtpClient = Substitute.For<ISmtpClientWrapper>();
        smtpClient.IsConnected.Returns(false);

        var opts = Options.Create(new EmailConnectorOptions
        {
            SmtpHost = "smtp.multi.com",
            SmtpPort = 587,
            UseTls = true,
            Username = "user",
            Password = "pass",
            DefaultFrom = "noreply@multi.com",
        });

        var connector = new EmailConnector(smtpClient, opts, NullLogger<EmailConnector>.Instance);
        var envelope = IntegrationEnvelope<string>.Create("Alert body", "AlertSvc", "system.alert");

        var recipients = new List<string>
        {
            "admin@multi.com",
            "ops@multi.com",
            "dev@multi.com",
        };

        await connector.SendAsync(
            envelope, recipients, "System Alert", p => p, CancellationToken.None);

        await smtpClient.Received(1).SendAsync(
            Arg.Any<MimeKit.MimeMessage>(), Arg.Any<CancellationToken>());
    }

    // ── Challenge 3: Email with Custom Subject Template ─────────────────────

    [Test]
    public async Task Challenge3_EmailWithCustomSubjectTemplate()
    {
        MimeKit.MimeMessage? capturedMessage = null;
        var smtpClient = Substitute.For<ISmtpClientWrapper>();
        smtpClient.IsConnected.Returns(false);
        smtpClient.SendAsync(Arg.Any<MimeKit.MimeMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => capturedMessage = ci.ArgAt<MimeKit.MimeMessage>(0));

        var opts = Options.Create(new EmailConnectorOptions
        {
            SmtpHost = "smtp.template.com",
            SmtpPort = 587,
            UseTls = true,
            Username = "user",
            Password = "pass",
            DefaultFrom = "noreply@template.com",
            DefaultSubjectTemplate = "[EIP] {MessageType} notification",
        });

        var connector = new EmailConnector(smtpClient, opts, NullLogger<EmailConnector>.Instance);
        var envelope = IntegrationEnvelope<string>.Create("Body", "Svc", "invoice.created");

        // Send with null subject to trigger template usage
        await connector.SendAsync(
            envelope, "dest@template.com", null, p => p, CancellationToken.None);

        Assert.That(capturedMessage, Is.Not.Null);
        Assert.That(capturedMessage!.Subject, Does.Contain("invoice.created"));
    }
}
