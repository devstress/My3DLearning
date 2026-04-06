// ============================================================================
// Tutorial 21 – Aggregator (Exam)
// ============================================================================
// E2E challenges: multi-group interleaved aggregation, metadata override on
// key conflict, and idempotent duplicate rejection.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Aggregator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial21;

[TestFixture]
public sealed class Exam
{
    [Test]
    public async Task Challenge1_InterleavedGroups_CompleteIndependently()
    {
        await using var output = new MockEndpoint("exam-agg");
        var aggregator = CreateAggregator(output, expectedCount: 2);

        var corrA = Guid.NewGuid();
        var corrB = Guid.NewGuid();

        var a1 = IntegrationEnvelope<string>.Create("a1", "svc", "t", corrA);
        var b1 = IntegrationEnvelope<string>.Create("b1", "svc", "t", corrB);
        var a2 = IntegrationEnvelope<string>.Create("a2", "svc", "t", corrA);
        var b2 = IntegrationEnvelope<string>.Create("b2", "svc", "t", corrB);

        Assert.That((await aggregator.AggregateAsync(a1)).IsComplete, Is.False);
        Assert.That((await aggregator.AggregateAsync(b1)).IsComplete, Is.False);
        Assert.That((await aggregator.AggregateAsync(a2)).IsComplete, Is.True);
        Assert.That((await aggregator.AggregateAsync(b2)).IsComplete, Is.True);

        output.AssertReceivedOnTopic("aggregated-topic", 2);
    }

    [Test]
    public async Task Challenge2_MetadataConflict_LaterOverridesEarlier()
    {
        await using var output = new MockEndpoint("exam-meta");
        var aggregator = CreateAggregator(output, expectedCount: 2);
        var correlationId = Guid.NewGuid();

        var e1 = IntegrationEnvelope<string>.Create("a", "svc", "t", correlationId) with
        {
            Metadata = new Dictionary<string, string> { ["key"] = "first" },
        };
        var e2 = IntegrationEnvelope<string>.Create("b", "svc", "t", correlationId) with
        {
            Metadata = new Dictionary<string, string> { ["key"] = "second" },
        };

        await aggregator.AggregateAsync(e1);
        var result = await aggregator.AggregateAsync(e2);

        Assert.That(result.AggregateEnvelope!.Metadata["key"], Is.EqualTo("second"));
        output.AssertReceivedOnTopic("aggregated-topic", 1);
    }

    [Test]
    public async Task Challenge3_DuplicateMessage_IsIdempotent()
    {
        await using var output = new MockEndpoint("exam-dup");
        var aggregator = CreateAggregator(output, expectedCount: 2);
        var correlationId = Guid.NewGuid();

        var e1 = IntegrationEnvelope<string>.Create("a", "svc", "t", correlationId);
        var e2 = IntegrationEnvelope<string>.Create("b", "svc", "t", correlationId);

        await aggregator.AggregateAsync(e1);
        // Resend e1 — duplicate by MessageId should be ignored
        var dupResult = await aggregator.AggregateAsync(e1);
        Assert.That(dupResult.IsComplete, Is.False);
        Assert.That(dupResult.ReceivedCount, Is.EqualTo(1));

        var final = await aggregator.AggregateAsync(e2);
        Assert.That(final.IsComplete, Is.True);
        Assert.That(final.ReceivedCount, Is.EqualTo(2));

        output.AssertReceivedOnTopic("aggregated-topic", 1);
    }

    private static MessageAggregator<string, string> CreateAggregator(
        MockEndpoint output, int expectedCount)
    {
        var store = new InMemoryMessageAggregateStore<string>();
        var completion = new CountCompletionStrategy<string>(expectedCount);
        var strategy = Substitute.For<IAggregationStrategy<string, string>>();
        strategy.Aggregate(Arg.Any<IReadOnlyList<string>>())
            .Returns(ci => string.Join(",", ci.Arg<IReadOnlyList<string>>()));

        var options = Options.Create(new AggregatorOptions
        {
            TargetTopic = "aggregated-topic",
            ExpectedCount = expectedCount,
        });

        return new MessageAggregator<string, string>(
            store, completion, strategy, output, options,
            NullLogger<MessageAggregator<string, string>>.Instance);
    }
}
