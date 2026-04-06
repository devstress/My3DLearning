// ============================================================================
// Tutorial 15 – Message Translator (Exam)
// ============================================================================
// Coding challenges: build a type-converting translator, verify metadata
// preservation, and implement a multi-field transformation pipeline.
// ============================================================================

using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.Processing.Translator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace TutorialLabs.Tutorial15;

// Simple DTOs used for type-conversion translation tests.
file sealed record OrderDto(string OrderId, decimal Amount, string Currency);
file sealed record OrderSummary(string Reference, string Total);

[TestFixture]
public sealed class Exam
{
    // ── Challenge 1: Type-Converting Translator ─────────────────────────────

    [Test]
    public async Task Challenge1_TypeConversion_OrderDtoToOrderSummary()
    {
        // Translate an OrderDto payload into an OrderSummary payload.
        // The translator should:
        //   - Map OrderId → Reference
        //   - Format Amount + Currency → Total (e.g. "100.50 USD")
        //   - Preserve CorrelationId and set CausationId
        var producer = Substitute.For<IMessageBrokerProducer>();

        var transform = new FuncPayloadTransform<OrderDto, OrderSummary>(order =>
            new OrderSummary(
                Reference: order.OrderId,
                Total: $"{order.Amount} {order.Currency}"));

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = "order-summaries",
            TargetMessageType = "order.summary",
        });

        var translator = new MessageTranslator<OrderDto, OrderSummary>(
            transform, producer, options,
            NullLogger<MessageTranslator<OrderDto, OrderSummary>>.Instance);

        var source = IntegrationEnvelope<OrderDto>.Create(
            new OrderDto("ORD-1", 250.75m, "EUR"),
            "OrderService",
            "order.created");

        var result = await translator.TranslateAsync(source);

        Assert.That(result.TranslatedEnvelope.Payload.Reference, Is.EqualTo("ORD-1"));
        Assert.That(result.TranslatedEnvelope.Payload.Total, Is.EqualTo("250.75 EUR"));
        Assert.That(result.TranslatedEnvelope.MessageType, Is.EqualTo("order.summary"));
        Assert.That(result.TranslatedEnvelope.CorrelationId, Is.EqualTo(source.CorrelationId));
        Assert.That(result.TranslatedEnvelope.CausationId, Is.EqualTo(source.MessageId));
        Assert.That(result.TargetTopic, Is.EqualTo("order-summaries"));
    }

    // ── Challenge 2: Metadata Preservation ──────────────────────────────────

    [Test]
    public async Task Challenge2_MetadataPreservation_AllMetadataCopied()
    {
        // Verify that the translator copies ALL metadata from the source envelope
        // to the translated envelope, including custom keys.
        var producer = Substitute.For<IMessageBrokerProducer>();
        var transform = new FuncPayloadTransform<string, string>(s => $"translated:{s}");

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = "output-topic",
        });

        var translator = new MessageTranslator<string, string>(
            transform, producer, options,
            NullLogger<MessageTranslator<string, string>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "data", "Service", "event.type") with
        {
            Priority = MessagePriority.High,
            SchemaVersion = "3.0",
            Metadata = new Dictionary<string, string>
            {
                ["tenant"] = "acme-corp",
                ["region"] = "eu-west",
                ["trace-id"] = "abc-123",
            },
        };

        var result = await translator.TranslateAsync(source);

        // Payload is transformed.
        Assert.That(result.TranslatedEnvelope.Payload, Is.EqualTo("translated:data"));

        // Metadata is preserved.
        Assert.That(result.TranslatedEnvelope.Metadata["tenant"], Is.EqualTo("acme-corp"));
        Assert.That(result.TranslatedEnvelope.Metadata["region"], Is.EqualTo("eu-west"));
        Assert.That(result.TranslatedEnvelope.Metadata["trace-id"], Is.EqualTo("abc-123"));

        // Priority and SchemaVersion are preserved.
        Assert.That(result.TranslatedEnvelope.Priority, Is.EqualTo(MessagePriority.High));
        Assert.That(result.TranslatedEnvelope.SchemaVersion, Is.EqualTo("3.0"));
    }

    // ── Challenge 3: FuncPayloadTransform Convenience ───────────────────────

    [Test]
    public async Task Challenge3_FuncPayloadTransform_SupportsComplexTransformations()
    {
        // Use FuncPayloadTransform to implement a transformation that:
        //   - Splits a comma-separated string into the count of elements
        //   - Returns the count as a string (e.g. "a,b,c" → "3")
        // Demonstrates that FuncPayloadTransform can wrap arbitrary logic.
        var producer = Substitute.For<IMessageBrokerProducer>();

        var transform = new FuncPayloadTransform<string, int>(csv =>
            csv.Split(',', StringSplitOptions.RemoveEmptyEntries).Length);

        var options = Options.Create(new TranslatorOptions
        {
            TargetTopic = "counts-topic",
            TargetMessageType = "item.count",
            TargetSource = "CounterService",
        });

        var translator = new MessageTranslator<string, int>(
            transform, producer, options,
            NullLogger<MessageTranslator<string, int>>.Instance);

        var source = IntegrationEnvelope<string>.Create(
            "apple,banana,cherry,date", "InventoryService", "inventory.list");

        var result = await translator.TranslateAsync(source);

        Assert.That(result.TranslatedEnvelope.Payload, Is.EqualTo(4));
        Assert.That(result.TranslatedEnvelope.MessageType, Is.EqualTo("item.count"));
        Assert.That(result.TranslatedEnvelope.Source, Is.EqualTo("CounterService"));
        Assert.That(result.TargetTopic, Is.EqualTo("counts-topic"));

        // Verify publish.
        await producer.Received(1).PublishAsync(
            Arg.Any<IntegrationEnvelope<int>>(),
            Arg.Is("counts-topic"),
            Arg.Any<CancellationToken>());
    }
}
