// ============================================================================
// Tutorial 22 – Scatter-Gather (Lab)
// ============================================================================
// EIP Pattern: Scatter-Gather.
// E2E: Wire real ScatterGatherer with MockEndpoint as producer.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.ScatterGather;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TutorialLabs.Infrastructure;

namespace TutorialLabs.Tutorial22;

[TestFixture]
public sealed class Lab
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp() => _output = new MockEndpoint("scatter-out");

    [TearDown]
    public async Task TearDown() => await _output.DisposeAsync();

    [Test]
    public async Task Scatter_PublishesToAllRecipients()
    {
        var sg = CreateScatterGatherer(timeoutMs: 500);
        var correlationId = Guid.NewGuid();
        var request = new ScatterRequest<string>(correlationId, "quote-request",
            new[] { "supplier-a", "supplier-b", "supplier-c" });

        // Start scatter-gather in background; submit responses immediately
        var task = sg.ScatterGatherAsync(request);

        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>("supplier-a", "price-a", DateTimeOffset.UtcNow, true, null));
        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>("supplier-b", "price-b", DateTimeOffset.UtcNow, true, null));
        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>("supplier-c", "price-c", DateTimeOffset.UtcNow, true, null));

        var result = await task;

        Assert.That(result.Responses.Count, Is.EqualTo(3));
        Assert.That(result.TimedOut, Is.False);
        _output.AssertReceivedOnTopic("supplier-a", 1);
        _output.AssertReceivedOnTopic("supplier-b", 1);
        _output.AssertReceivedOnTopic("supplier-c", 1);
    }

    [Test]
    public async Task Scatter_EmptyRecipients_ReturnsImmediately()
    {
        var sg = CreateScatterGatherer(timeoutMs: 500);
        var request = new ScatterRequest<string>(Guid.NewGuid(), "data", Array.Empty<string>());

        var result = await sg.ScatterGatherAsync(request);

        Assert.That(result.Responses.Count, Is.EqualTo(0));
        Assert.That(result.TimedOut, Is.False);
        _output.AssertNoneReceived();
    }

    [Test]
    public async Task Gather_TimesOut_ReturnsPartialResponses()
    {
        var sg = CreateScatterGatherer(timeoutMs: 200);
        var correlationId = Guid.NewGuid();
        var request = new ScatterRequest<string>(correlationId, "req",
            new[] { "fast", "slow" });

        var task = sg.ScatterGatherAsync(request);

        // Only fast responds
        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>("fast", "done", DateTimeOffset.UtcNow, true, null));

        var result = await task;

        Assert.That(result.TimedOut, Is.True);
        Assert.That(result.Responses.Count, Is.EqualTo(1));
        Assert.That(result.Responses[0].Recipient, Is.EqualTo("fast"));
    }

    [Test]
    public async Task Gather_PreservesCorrelationId()
    {
        var sg = CreateScatterGatherer(timeoutMs: 500);
        var correlationId = Guid.NewGuid();
        var request = new ScatterRequest<string>(correlationId, "data",
            new[] { "topic-1" });

        var task = sg.ScatterGatherAsync(request);
        await sg.SubmitResponseAsync(correlationId,
            new GatherResponse<string>("topic-1", "resp", DateTimeOffset.UtcNow, true, null));

        var result = await task;

        Assert.That(result.CorrelationId, Is.EqualTo(correlationId));
    }

    [Test]
    public async Task SubmitResponse_UnknownCorrelation_ReturnsFalse()
    {
        var sg = CreateScatterGatherer(timeoutMs: 500);

        var accepted = await sg.SubmitResponseAsync(Guid.NewGuid(),
            new GatherResponse<string>("x", "data", DateTimeOffset.UtcNow, true, null));

        Assert.That(accepted, Is.False);
    }

    [Test]
    public async Task Scatter_ExceedsMaxRecipients_Throws()
    {
        var sg = CreateScatterGatherer(timeoutMs: 500, maxRecipients: 2);
        var request = new ScatterRequest<string>(Guid.NewGuid(), "data",
            new[] { "a", "b", "c" });

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await sg.ScatterGatherAsync(request));
    }

    private ScatterGatherer<string, string> CreateScatterGatherer(
        int timeoutMs, int maxRecipients = 50)
    {
        var options = Options.Create(new ScatterGatherOptions
        {
            TimeoutMs = timeoutMs,
            MaxRecipients = maxRecipients,
        });

        return new ScatterGatherer<string, string>(
            _output, options,
            NullLogger<ScatterGatherer<string, string>>.Instance);
    }
}
