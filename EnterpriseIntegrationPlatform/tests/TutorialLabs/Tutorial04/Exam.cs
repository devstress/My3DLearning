// ============================================================================
// Tutorial 04 – Integration Envelope (Exam)
// ============================================================================
// EIP Pattern: Envelope Wrapper
// End-to-End: Full metadata through PointToPointChannel, multi-hop causation
// chains, and split-message sequences — all verified at MockEndpoint output.
// ============================================================================

using NUnit.Framework;
using TutorialLabs.Infrastructure;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion.Channels;
using Microsoft.Extensions.Logging.Abstractions;

namespace TutorialLabs.Tutorial04;

[TestFixture]
public sealed class Exam
{
    private MockEndpoint _output = null!;

    [SetUp]
    public void SetUp()
    {
        _output = new MockEndpoint("output");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _output.DisposeAsync();
    }

    [Test]
    public async Task EndToEnd_FullMetadata_ThroughPointToPointChannel()
    {
        var channel = new PointToPointChannel(
            _output, _output, NullLogger<PointToPointChannel>.Instance);

        var envelope = IntegrationEnvelope<string>.Create(
            "full-metadata", "MetadataService", "metadata.test") with
        {
            Priority = MessagePriority.High,
            Intent = MessageIntent.Command,
            ReplyTo = "reply-topic",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            SequenceNumber = 0,
            TotalCount = 1,
            Metadata = new Dictionary<string, string>
            {
                [MessageHeaders.TraceId] = "trace-001",
                [MessageHeaders.SpanId] = "span-001",
                [MessageHeaders.ContentType] = "application/json",
            },
        };

        await channel.SendAsync(envelope, "metadata-queue", CancellationToken.None);

        _output.AssertReceivedCount(1);
        var received = _output.GetReceived<string>();
        Assert.That(received.Priority, Is.EqualTo(MessagePriority.High));
        Assert.That(received.ReplyTo, Is.EqualTo("reply-topic"));
        Assert.That(received.Metadata[MessageHeaders.TraceId], Is.EqualTo("trace-001"));
        Assert.That(received.ExpiresAt, Is.Not.Null);
    }

    [Test]
    public async Task EndToEnd_MultiHopCausation_AllLinksPreserved()
    {
        var envelopeA = IntegrationEnvelope<string>.Create(
            "PlaceOrder", "WebApp", "order.place") with
        {
            Intent = MessageIntent.Command,
        };

        var envelopeB = IntegrationEnvelope<string>.Create(
            "OrderPlaced", "OrderService", "order.placed",
            correlationId: envelopeA.CorrelationId,
            causationId: envelopeA.MessageId) with
        {
            Intent = MessageIntent.Event,
        };

        var envelopeC = IntegrationEnvelope<string>.Create(
            "InvoiceGenerated", "BillingService", "invoice.generated",
            correlationId: envelopeA.CorrelationId,
            causationId: envelopeB.MessageId) with
        {
            Intent = MessageIntent.Document,
        };

        await _output.PublishAsync(envelopeA, "commands");
        await _output.PublishAsync(envelopeB, "events");
        await _output.PublishAsync(envelopeC, "documents");

        _output.AssertReceivedCount(3);
        var rA = _output.GetReceived<string>(0);
        var rB = _output.GetReceived<string>(1);
        var rC = _output.GetReceived<string>(2);

        Assert.That(rA.CausationId, Is.Null);
        Assert.That(rB.CausationId, Is.EqualTo(rA.MessageId));
        Assert.That(rC.CausationId, Is.EqualTo(rB.MessageId));
        Assert.That(rB.CorrelationId, Is.EqualTo(rA.CorrelationId));
        Assert.That(rC.CorrelationId, Is.EqualTo(rA.CorrelationId));
    }

    [Test]
    public async Task EndToEnd_SplitSequence_AllPartsPreserved()
    {
        var correlationId = Guid.NewGuid();
        const int total = 5;

        for (var i = 0; i < total; i++)
        {
            var part = IntegrationEnvelope<string>.Create(
                $"chunk-{i}", "Splitter", "data.chunk",
                correlationId: correlationId) with
            {
                SequenceNumber = i,
                TotalCount = total,
            };
            await _output.PublishAsync(part, "chunks");
        }

        _output.AssertReceivedCount(total);
        var all = _output.GetAllReceived<string>("chunks");

        for (var i = 0; i < total; i++)
        {
            Assert.That(all[i].SequenceNumber, Is.EqualTo(i));
            Assert.That(all[i].TotalCount, Is.EqualTo(total));
            Assert.That(all[i].Payload, Is.EqualTo($"chunk-{i}"));
            Assert.That(all[i].CorrelationId, Is.EqualTo(correlationId));
        }
    }
}
