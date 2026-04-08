// ============================================================================
// Tutorial 36 – Email Connector (Exam · Fill in the Blanks)
// ============================================================================
// INSTRUCTIONS: Each test has TODO comments where you must write the missing
//   code. Run the tests — they will FAIL until you fill in the blanks.
//   Check your work against Exam.Answers.cs after attempting each challenge.
//
// DIFFICULTY TIERS:
//   🟢 Starter       — full smtp lifecycle_ connect auth send disconnect
//   🟡 Intermediate  — multi recipient_ mime message contains all addresses
//   🔴 Advanced      — mock endpoint_ custom subject template
// ============================================================================
#pragma warning disable CS0219  // Variable assigned but never used
#pragma warning disable CS8602  // Dereference of possibly null reference
#pragma warning disable CS8604  // Possible null reference argument

using EnterpriseIntegrationPlatform.Connector.Email;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EnterpriseIntegrationPlatform.Testing;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

#if EXAM_STUDENT
namespace TutorialLabs.Tutorial36;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Starter_FullSmtpLifecycle_ConnectAuthSendDisconnect()
    {
        // TODO: Create a MockSmtpClient with appropriate configuration
        dynamic smtp = null!;
        // TODO: Create a EmailConnector with appropriate configuration
        dynamic connector = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;

        // TODO: await connector.SendAsync(...)

        smtp.AssertLifecycleOrder();
    }

    [Test]
    public async Task Intermediate_MultiRecipient_MimeMessageContainsAllAddresses()
    {
        // TODO: Create a MockSmtpClient with appropriate configuration
        dynamic smtp = null!;

        // TODO: Create a EmailConnector with appropriate configuration
        dynamic connector = null!;

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic envelope = null!;
        // TODO: Create a List with appropriate configuration
        dynamic recipients = null!;

        // TODO: await connector.SendAsync(...)

        var captured = smtp.LastSentMessage;
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.To.Count, Is.EqualTo(3));
        Assert.That(smtp.SendCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Advanced_MockEndpoint_CustomSubjectTemplate()
    {
        await using var input = new MockEndpoint("exam-email-in");
        // TODO: Create a MockSmtpClient with appropriate configuration
        dynamic smtp = null!;

        // TODO: Create a EmailConnector with appropriate configuration
        dynamic connector = null!;

        await input.SubscribeAsync<string>("email-topic", "email-group",
            async envelope =>
            {
                await connector.SendAsync(envelope, "dest@tpl.com", null, p => p, CancellationToken.None);
            });

        // TODO: Create an IntegrationEnvelope with appropriate payload, source, and message type
        dynamic env = null!;
        // TODO: await input.SendAsync(...)

        var captured = smtp.LastSentMessage;
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Subject, Is.EqualTo("[EIP] invoice.created notification"));
    }
}
#endif
