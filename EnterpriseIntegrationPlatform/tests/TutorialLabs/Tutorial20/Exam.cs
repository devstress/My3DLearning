// ============================================================================
// Tutorial 20 – Splitter (Exam)
// ============================================================================
// Coding challenges: split a JSON object with a named array property,
// verify metadata/priority preservation across split envelopes, and use
// TargetMessageType override.
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
public sealed class Exam
{
    // ── Challenge 1: Named Array Property Split ─────────────────────────────

    [Test]
    public async Task Challenge1_NamedArrayProperty_SplitsCorrectly()
    {
        // The payload is a JSON object with an "items" array property.
        // Use JsonArraySplitStrategy with ArrayPropertyName set to split only
        // the items array into individual envelopes.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var splitOptions = Options.Create(new SplitterOptions
        {
            TargetTopic = "order-items",
            ArrayPropertyName = "items",
        });

        var strategy = new JsonArraySplitStrategy(splitOptions);
        var splitter = new MessageSplitter<JsonElement>(
            strategy, producer, splitOptions,
            NullLogger<MessageSplitter<JsonElement>>.Instance);

        var payload = JsonSerializer.Deserialize<JsonElement>("""
            {
                "orderId": "ORD-1",
                "items": [
                    {"sku": "SKU-A", "qty": 2},
                    {"sku": "SKU-B", "qty": 5},
                    {"sku": "SKU-C", "qty": 1}
                ]
            }
            """);

        var source = IntegrationEnvelope<JsonElement>.Create(
            payload, "OrderService", "order.batch");

        var result = await splitter.SplitAsync(source);

        Assert.That(result.ItemCount, Is.EqualTo(3));
        Assert.That(result.SplitEnvelopes[0].Payload.GetProperty("sku").GetString(),
            Is.EqualTo("SKU-A"));
        Assert.That(result.SplitEnvelopes[1].Payload.GetProperty("qty").GetInt32(),
            Is.EqualTo(5));
        Assert.That(result.SplitEnvelopes[2].Payload.GetProperty("sku").GetString(),
            Is.EqualTo("SKU-C"));
    }

    // ── Challenge 2: Metadata and Priority Preservation ─────────────────────

    [Test]
    public async Task Challenge2_MetadataAndPriority_PreservedInSplitEnvelopes()
    {
        // Verify that metadata, priority, and schema version from the source
        // envelope are copied to every split envelope.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var strategy = new FuncSplitStrategy<string>(
            composite => composite.Split(';').ToList());

        var options = Options.Create(new SplitterOptions { TargetTopic = "split-out" });
        var splitter = new MessageSplitter<string>(
            strategy, producer, options,
            NullLogger<MessageSplitter<string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "A;B;C", "BatchService", "batch.items") with
        {
            Priority = MessagePriority.High,
            SchemaVersion = "2.0",
            Metadata = new Dictionary<string, string>
            {
                ["tenant"] = "acme",
                ["region"] = "us-east",
            },
        };

        var result = await splitter.SplitAsync(source);

        Assert.That(result.ItemCount, Is.EqualTo(3));

        foreach (var env in result.SplitEnvelopes)
        {
            Assert.That(env.Priority, Is.EqualTo(MessagePriority.High));
            Assert.That(env.SchemaVersion, Is.EqualTo("2.0"));
            Assert.That(env.Metadata["tenant"], Is.EqualTo("acme"));
            Assert.That(env.Metadata["region"], Is.EqualTo("us-east"));
            Assert.That(env.CorrelationId, Is.EqualTo(source.CorrelationId));
            Assert.That(env.CausationId, Is.EqualTo(source.MessageId));
        }
    }

    // ── Challenge 3: TargetMessageType Override ─────────────────────────────

    [Test]
    public async Task Challenge3_TargetMessageTypeOverride_AppliedToSplitEnvelopes()
    {
        // When TargetMessageType is configured, all split envelopes should use
        // the overridden message type instead of the source's message type.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var strategy = new FuncSplitStrategy<string>(s => s.Split(',').ToList());

        var options = Options.Create(new SplitterOptions
        {
            TargetTopic = "individual-items",
            TargetMessageType = "item.created",
            TargetSource = "SplitterService",
        });

        var splitter = new MessageSplitter<string>(
            strategy, producer, options,
            NullLogger<MessageSplitter<string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "X,Y", "BatchService", "batch.submitted");

        var result = await splitter.SplitAsync(source);

        Assert.That(result.ItemCount, Is.EqualTo(2));
        Assert.That(result.TargetTopic, Is.EqualTo("individual-items"));

        foreach (var env in result.SplitEnvelopes)
        {
            Assert.That(env.MessageType, Is.EqualTo("item.created"));
            Assert.That(env.Source, Is.EqualTo("SplitterService"));
        }

        await producer.Received(2).PublishAsync(
            Arg.Any<IntegrationEnvelope<string>>(),
            Arg.Is("individual-items"),
            Arg.Any<CancellationToken>());
    }
}
