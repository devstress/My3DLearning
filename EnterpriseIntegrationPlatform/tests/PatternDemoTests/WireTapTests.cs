using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Wire Tap pattern.
/// Inspects messages flowing through the system without altering them.
/// BizTalk equivalent: Tracking (HAT), MessageBox subscriptions for monitoring.
/// EIP: Wire Tap (p. 547)
/// </summary>
public class WireTapTests
{
    private record AuditPayload(string Action, string UserId);

    [Fact]
    public async Task Captures_Messages_WithoutAlteringFlow()
    {
        var tap = new InMemoryWireTap<AuditPayload>();

        var envelope = IntegrationEnvelope<AuditPayload>.Create(
            new AuditPayload("Login", "user-42"), "AuthService", "UserAction");

        // The wire tap captures the message
        await tap.TapAsync(envelope);

        // Assert — message was captured for monitoring
        tap.TappedMessages.Should().HaveCount(1);
        tap.TappedMessages[0].Payload.Action.Should().Be("Login");
    }

    [Fact]
    public async Task Captures_Multiple_Messages_InOrder()
    {
        var tap = new InMemoryWireTap<AuditPayload>();

        await tap.TapAsync(IntegrationEnvelope<AuditPayload>.Create(
            new AuditPayload("Login", "user-1"), "Auth", "UserAction"));
        await tap.TapAsync(IntegrationEnvelope<AuditPayload>.Create(
            new AuditPayload("Purchase", "user-1"), "Orders", "UserAction"));
        await tap.TapAsync(IntegrationEnvelope<AuditPayload>.Create(
            new AuditPayload("Logout", "user-1"), "Auth", "UserAction"));

        tap.TappedMessages.Should().HaveCount(3);
        tap.TappedMessages.Select(m => m.Payload.Action)
            .Should().ContainInOrder("Login", "Purchase", "Logout");
    }
}
