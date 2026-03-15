using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Message Priority pattern.
/// Messages carry priority metadata that consumers use to order processing.
/// BizTalk equivalent: Priority-based subscription filters on Send Ports.
/// EIP: Related to Message Channel selection (p. 60)
/// </summary>
public class MessagePriorityTests
{
    private record AlertPayload(string AlertType, string Description);

    [Fact]
    public void Messages_HavePriorityLevels()
    {
        var critical = new IntegrationEnvelope<AlertPayload>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "Monitoring",
            MessageType = "Alert",
            Priority = MessagePriority.Critical,
            Payload = new AlertPayload("SystemDown", "Production database unreachable"),
        };

        var normal = IntegrationEnvelope<AlertPayload>.Create(
            new AlertPayload("InfoUpdate", "Scheduled maintenance"),
            "Monitoring", "Alert");

        critical.Priority.Should().Be(MessagePriority.Critical);
        normal.Priority.Should().Be(MessagePriority.Normal);

        // Critical priority has higher numeric value
        ((int)critical.Priority).Should().BeGreaterThan((int)normal.Priority);
    }

    [Fact]
    public void PriorityLevels_AreOrdered()
    {
        ((int)MessagePriority.Low).Should().BeLessThan((int)MessagePriority.Normal);
        ((int)MessagePriority.Normal).Should().BeLessThan((int)MessagePriority.High);
        ((int)MessagePriority.High).Should().BeLessThan((int)MessagePriority.Critical);
    }
}
