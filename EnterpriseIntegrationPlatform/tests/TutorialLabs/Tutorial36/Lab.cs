// ============================================================================
// Tutorial 36 – Email Connector (Lab)
// ============================================================================
// EIP Pattern: Connector.
// E2E: Wire real EmailConnector with MockSmtpClient and
// MockEndpoint to simulate envelope-driven email dispatch.
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
public sealed class Lab
{
    private MockEndpoint _input = null!;
    private MockSmtpClient _smtp = null!;

    [SetUp]
    public void SetUp()
    {
        _input = new MockEndpoint("email-in");
        _smtp = new MockSmtpClient();
    }

    [TearDown]
    public async Task TearDown() => await _input.DisposeAsync();

    [Test]
    public async Task Send_SingleRecipient_DelegatesToSmtp()
    {
        var connector = CreateConnector();
        var envelope = IntegrationEnvelope<string>.Create("Hello", "Svc", "order.placed");

        await connector.SendAsync(envelope, "user@test.com", "Test", p => p, CancellationToken.None);

        Assert.That(_smtp.Calls.Count(c => c.Operation == "Connect"), Is.EqualTo(1));
        Assert.That(_smtp.SendCount, Is.EqualTo(1));
        Assert.That(_smtp.Calls.Count(c => c.Operation == "Disconnect"), Is.EqualTo(1));
    }

    [Test]
    public async Task Send_MultipleRecipients_SingleSmtpSend()
    {
        var connector = CreateConnector();
        var envelope = IntegrationEnvelope<string>.Create("Alert", "AlertSvc", "system.alert");
        var recipients = new List<string> { "a@test.com", "b@test.com", "c@test.com" };

        await connector.SendAsync(envelope, recipients, "Alert", p => p, CancellationToken.None);

        Assert.That(_smtp.SendCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Send_NullSubject_UsesDefaultTemplate()
    {
        var connector = CreateConnector();
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "invoice.created");

        await connector.SendAsync(envelope, "to@test.com", null, p => p, CancellationToken.None);

        var captured = _smtp.LastSentMessage;
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Subject, Is.EqualTo("invoice.created notification"));
    }

    [Test]
    public async Task Send_InjectsCorrelationHeaders()
    {
        var connector = CreateConnector();
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "order.shipped");

        await connector.SendAsync(envelope, "to@test.com", "Shipped", p => p, CancellationToken.None);

        var captured = _smtp.LastSentMessage;
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Headers["X-Correlation-Id"],
            Is.EqualTo(envelope.CorrelationId.ToString()));
        Assert.That(captured.Headers["X-Message-Id"],
            Is.EqualTo(envelope.MessageId.ToString()));
    }

    [Test]
    public async Task Send_DisconnectsEvenWhenAuthThrows()
    {
        _smtp.WithAuthFailure(new InvalidOperationException("Auth failed"));

        var connector = CreateConnector();
        var envelope = IntegrationEnvelope<string>.Create("data", "Svc", "test.event");

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await connector.SendAsync(envelope, "to@test.com", "Sub", p => p, CancellationToken.None));

        Assert.That(_smtp.Calls.Count(c => c.Operation == "Disconnect"), Is.EqualTo(1));
    }

    [Test]
    public async Task E2E_MockEndpoint_FeedsEnvelope_ToEmailConnector()
    {
        var connector = CreateConnector();

        // Subscribe handler on MockEndpoint that triggers email send
        await _input.SubscribeAsync<string>("email-topic", "email-group",
            async envelope =>
            {
                await connector.SendAsync(envelope, "dest@test.com", null, p => p, CancellationToken.None);
            });

        // Feed envelope through MockEndpoint
        var env = IntegrationEnvelope<string>.Create("Order confirmed", "OrderSvc", "order.confirmed");
        await _input.SendAsync(env);

        Assert.That(_smtp.SendCount, Is.EqualTo(1));
        var captured = _smtp.LastSentMessage;
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Subject, Is.EqualTo("order.confirmed notification"));
    }

    private EmailConnector CreateConnector() =>
        new(_smtp, Options.Create(new EmailConnectorOptions
        {
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            UseTls = true,
            Username = "user",
            Password = "pass",
            DefaultFrom = "noreply@test.com",
        }), NullLogger<EmailConnector>.Instance);
}
