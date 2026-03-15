using FluentAssertions;
using Xunit;

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Routing;

namespace EnterpriseIntegrationPlatform.Tests.PatternDemo;

/// <summary>
/// Demonstrates the Resequencer pattern.
/// Reorders messages that arrive out of sequence.
/// BizTalk equivalent: Sequential Convoy with ordered delivery.
/// EIP: Resequencer (p. 283)
/// </summary>
public class ResequencerTests
{
    private record EventPayload(int EventNumber, string Description);

    [Fact]
    public void Reorders_OutOfSequence_Messages()
    {
        var resequencer = new Resequencer<EventPayload>(startSequence: 0);

        // Messages arrive out of order: 2, 0, 1
        resequencer.Add(
            IntegrationEnvelope<EventPayload>.Create(
                new EventPayload(2, "Third"), "Source", "Event"),
            sequenceNumber: 2);

        // No messages ready yet (waiting for 0)
        resequencer.ReleaseInOrder().Should().BeEmpty();

        resequencer.Add(
            IntegrationEnvelope<EventPayload>.Create(
                new EventPayload(0, "First"), "Source", "Event"),
            sequenceNumber: 0);

        // Sequence 0 is ready, but 1 is still missing
        var released = resequencer.ReleaseInOrder();
        released.Should().HaveCount(1);
        released[0].Payload.EventNumber.Should().Be(0);

        resequencer.Add(
            IntegrationEnvelope<EventPayload>.Create(
                new EventPayload(1, "Second"), "Source", "Event"),
            sequenceNumber: 1);

        // Now 1 and 2 are both ready
        released = resequencer.ReleaseInOrder();
        released.Should().HaveCount(2);
        released[0].Payload.EventNumber.Should().Be(1);
        released[1].Payload.EventNumber.Should().Be(2);
    }
}
