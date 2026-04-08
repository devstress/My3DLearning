// ============================================================================
// Tutorial 36 – Email Connector (Exam Answers · DO NOT PEEK)
// ============================================================================
// These are the complete, passing answers for the Exam.
// Try to solve each challenge yourself before looking here.
//
// DIFFICULTY TIERS:
//   🟢 Starter      — full smtp lifecycle_ connect auth send disconnect
//   🟡 Intermediate — multi recipient_ mime message contains all addresses
//   🔴 Advanced     — mock endpoint_ custom subject template
// ============================================================================

using EnterpriseIntegrationPlatform.Connector.Email;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial36;

[TestFixture]
public sealed class ExamAnswers
{
    [Test]
    public async Task Starter_FullSmtpLifecycle_ConnectAuthSendDisconnect()
    {
        var smtp = new MockSmtpClient();
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

        smtp.AssertLifecycleOrder();
    }

    [Test]
    public async Task Intermediate_MultiRecipient_MimeMessageContainsAllAddresses()
    {
        var smtp = new MockSmtpClient();

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

        var captured = smtp.LastSentMessage;
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.To.Count, Is.EqualTo(3));
        Assert.That(smtp.SendCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Advanced_MockEndpoint_CustomSubjectTemplate()
    {
        await using var input = new MockEndpoint("exam-email-in");
        var smtp = new MockSmtpClient();

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

        var captured = smtp.LastSentMessage;
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Subject, Is.EqualTo("[EIP] invoice.created notification"));
    }
}
