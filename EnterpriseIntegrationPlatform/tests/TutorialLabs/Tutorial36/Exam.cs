// ============================================================================
// Tutorial 36 – Email Connector (Exam)
// ============================================================================
// E2E challenges: ordered SMTP lifecycle, multi-recipient MIME verification,
// and MockEndpoint-driven custom subject template.
// ============================================================================

using EnterpriseIntegrationPlatform.Connector.Email;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using NSubstitute;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial36;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_FullSmtpLifecycle_ConnectAuthSendDisconnect()
    {
        var smtp = Substitute.For<ISmtpClientWrapper>();
        var connector = new EmailConnector(smtp,
            Options.Create(new EmailConnectorOptions
            {
                SmtpHost = "smtp.lifecycle.com", SmtpPort = 587, UseTls = true,
                Username = "admin", Password = "s3cret",
                DefaultFrom = "system@lifecycle.com",
            }),
            NullLogger<EmailConnector>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "Order confirmed", "OrderSvc", "order.confirmed");

        await connector.SendAsync(
            envelope, "customer@example.com", "Order Update", p => p, CancellationToken.None);

        Received.InOrder(() =>
        {
            smtp.ConnectAsync("smtp.lifecycle.com", 587, true, Arg.Any<CancellationToken>());
            smtp.AuthenticateAsync("admin", "s3cret", Arg.Any<CancellationToken>());
            smtp.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>());
            smtp.DisconnectAsync(true, Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task Challenge2_MultiRecipient_MimeMessageContainsAllAddresses()
    {
        MimeMessage? captured = null;
        var smtp = Substitute.For<ISmtpClientWrapper>();
        smtp.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => captured = ci.ArgAt<MimeMessage>(0));

        var connector = new EmailConnector(smtp,
            Options.Create(new EmailConnectorOptions
            {
                SmtpHost = "smtp.multi.com", SmtpPort = 587, UseTls = true,
                Username = "user", Password = "pass",
                DefaultFrom = "noreply@multi.com",
            }),
            NullLogger<EmailConnector>.Instance);

        var envelope = IntegrationEnvelope<string>.Create("Alert body", "AlertSvc", "system.alert");
        var recipients = new List<string> { "admin@multi.com", "ops@multi.com", "dev@multi.com" };

        await connector.SendAsync(envelope, recipients, "System Alert", p => p, CancellationToken.None);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.To.Count, Is.EqualTo(3));
        await smtp.Received(1).SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Challenge3_MockEndpoint_CustomSubjectTemplate()
    {
        await using var input = new MockEndpoint("exam-email-in");
        MimeMessage? captured = null;
        var smtp = Substitute.For<ISmtpClientWrapper>();
        smtp.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => captured = ci.ArgAt<MimeMessage>(0));

        var connector = new EmailConnector(smtp,
            Options.Create(new EmailConnectorOptions
            {
                SmtpHost = "smtp.tpl.com", SmtpPort = 587, UseTls = true,
                Username = "user", Password = "pass",
                DefaultFrom = "noreply@tpl.com",
                DefaultSubjectTemplate = "[EIP] {MessageType} notification",
            }),
            NullLogger<EmailConnector>.Instance);

        await input.SubscribeAsync<string>("email-topic", "email-group",
            async envelope =>
            {
                await connector.SendAsync(envelope, "dest@tpl.com", null, p => p, CancellationToken.None);
            });

        var env = IntegrationEnvelope<string>.Create("Body", "Svc", "invoice.created");
        await input.SendAsync(env);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Subject, Is.EqualTo("[EIP] invoice.created notification"));
    }
}
