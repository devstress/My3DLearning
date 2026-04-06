// ============================================================================
// Tutorial 20 – Splitter (Lab)
// ============================================================================
// This lab exercises the MessageSplitter — the pattern that decomposes a
// composite message into individual messages, each published separately.
// You will test FuncSplitStrategy, JsonArraySplitStrategy, envelope field
// preservation, and error handling for unconfigured target topics.
// ============================================================================

using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Splitter;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial20;

[TestFixture]
public sealed class Lab
{
    // ── Basic Split with FuncSplitStrategy ───────────────────────────────────

    [Test]
    public async Task Split_FuncStrategy_SplitsIntoIndividualEnvelopes()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var strategy = new FuncSplitStrategy<string>(
            composite => composite.Split(',').ToList());

        var options = Options.Create(new SplitterOptions { TargetTopic = "items-topic" });
        var splitter = new MessageSplitter<string>(
            strategy, producer, options,
            NullLogger<MessageSplitter<string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "apple,banana,cherry", "InventoryService", "batch.items");

        var result = await splitter.SplitAsync(source);

        Assert.That(result.ItemCount, Is.EqualTo(3));
        Assert.That(result.TargetTopic, Is.EqualTo("items-topic"));
        Assert.That(result.SourceMessageId, Is.EqualTo(source.MessageId));
        Assert.That(result.SplitEnvelopes[0].Payload, Is.EqualTo("apple"));
        Assert.That(result.SplitEnvelopes[1].Payload, Is.EqualTo("banana"));
        Assert.That(result.SplitEnvelopes[2].Payload, Is.EqualTo("cherry"));
    }

    // ── CorrelationId and CausationId Preservation ──────────────────────────

    [Test]
    public async Task Split_PreservesCorrelationId_SetsCausationId()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var strategy = new FuncSplitStrategy<string>(s => new[] { s });

        var options = Options.Create(new SplitterOptions { TargetTopic = "topic" });
        var splitter = new MessageSplitter<string>(
            strategy, producer, options,
            NullLogger<MessageSplitter<string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "payload", "Service", "event.type");

        var result = await splitter.SplitAsync(source);

        var splitEnv = result.SplitEnvelopes[0];
        Assert.That(splitEnv.CorrelationId, Is.EqualTo(source.CorrelationId));
        Assert.That(splitEnv.CausationId, Is.EqualTo(source.MessageId));
        Assert.That(splitEnv.MessageId, Is.Not.EqualTo(source.MessageId));
    }

    // ── Publisher Called for Each Split Envelope ─────────────────────────────

    [Test]
    public async Task Split_PublishesEachEnvelopeToTargetTopic()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var strategy = new FuncSplitStrategy<string>(
            s => s.Split('|').ToList());

        var options = Options.Create(new SplitterOptions { TargetTopic = "split-topic" });
        var splitter = new MessageSplitter<string>(
            strategy, producer, options,
            NullLogger<MessageSplitter<string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "A|B", "Service", "batch");

        await splitter.SplitAsync(source);

        await producer.Received(2).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("split-topic"),
            Arg.Any<CancellationToken>());
    }

    // ── No Target Topic Configured — Throws ─────────────────────────────────

    [Test]
    public void Split_NoTargetTopic_ThrowsInvalidOperationException()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var strategy = new FuncSplitStrategy<string>(s => new[] { s });

        var options = Options.Create(new SplitterOptions { TargetTopic = "" });
        var splitter = new MessageSplitter<string>(
            strategy, producer, options,
            NullLogger<MessageSplitter<string>>.Instance);

        var source = IntegrationEnvelope<string>.Create("data", "Svc", "evt");

        Assert.ThrowsAsync<InvalidOperationException>(
            () => splitter.SplitAsync(source));
    }

    // ── Zero Items After Split ──────────────────────────────────────────────

    [Test]
    public async Task Split_ZeroItems_ReturnsEmptyResult_NoPublish()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var strategy = new FuncSplitStrategy<string>(_ => Array.Empty<string>());

        var options = Options.Create(new SplitterOptions { TargetTopic = "topic" });
        var splitter = new MessageSplitter<string>(
            strategy, producer, options,
            NullLogger<MessageSplitter<string>>.Instance);

        var source = IntegrationEnvelope<string>.Create("empty", "Svc", "evt");

        var result = await splitter.SplitAsync(source);

        Assert.That(result.ItemCount, Is.EqualTo(0));
        Assert.That(result.SplitEnvelopes, Is.Empty);
        await producer.DidNotReceive()
            .PublishAsync(Arg.Any<IntegrationEnvelope<string>>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── JsonArraySplitStrategy ──────────────────────────────────────────────

    [Test]
    public async Task Split_JsonArrayStrategy_SplitsTopLevelArray()
    {
        var producer = Substitute.For<IMessageBrokerProducer>();
        var splitOptions = Options.Create(new SplitterOptions { TargetTopic = "json-items" });
        var strategy = new JsonArraySplitStrategy(splitOptions);

        var splitter = new MessageSplitter<JsonElement>(
            strategy, producer, splitOptions,
            NullLogger<MessageSplitter<JsonElement>>.Instance);

        var jsonArray = JsonSerializer.Deserialize<JsonElement>(
            """[{"id":1},{"id":2},{"id":3}]""");

        var source = IntegrationEnvelope<JsonElement>.Create(
            jsonArray, "BatchService", "batch.created");

        var result = await splitter.SplitAsync(source);

        Assert.That(result.ItemCount, Is.EqualTo(3));
        Assert.That(result.SplitEnvelopes[0].Payload.GetProperty("id").GetInt32(), Is.EqualTo(1));
        Assert.That(result.SplitEnvelopes[1].Payload.GetProperty("id").GetInt32(), Is.EqualTo(2));
        Assert.That(result.SplitEnvelopes[2].Payload.GetProperty("id").GetInt32(), Is.EqualTo(3));
    }
}
