using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Processing.Resequencer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class ResequencerTests
{
    private MessageResequencer _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new MessageResequencer(
            Options.Create(new ResequencerOptions()),
            NullLogger<MessageResequencer>.Instance);
    }

    private static IntegrationEnvelope<string> CreateSequencedEnvelope(
        Guid correlationId,
        int sequenceNumber,
        int totalCount,
        string payload = "data")
    {
        return new IntegrationEnvelope<string>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow,
            Source = "Test",
            MessageType = "OrderItem",
            Payload = payload,
            SequenceNumber = sequenceNumber,
            TotalCount = totalCount,
        };
    }

    [Test]
    public void Accept_InOrderSequence_ReleasesAllAtEnd()
    {
        var correlationId = Guid.NewGuid();
        var e0 = CreateSequencedEnvelope(correlationId, 0, 3, "first");
        var e1 = CreateSequencedEnvelope(correlationId, 1, 3, "second");
        var e2 = CreateSequencedEnvelope(correlationId, 2, 3, "third");

        var result0 = _sut.Accept(e0);
        var result1 = _sut.Accept(e1);
        var result2 = _sut.Accept(e2);

        Assert.That(result0, Is.Empty, "first message buffered");
        Assert.That(result1, Is.Empty, "second message buffered");
        Assert.That(result2, Has.Count.EqualTo(3), "third message completes sequence");
        Assert.That(result2[0].Payload, Is.EqualTo("first"));
        Assert.That(result2[1].Payload, Is.EqualTo("second"));
        Assert.That(result2[2].Payload, Is.EqualTo("third"));
    }

    [Test]
    public void Accept_OutOfOrder_ReleasesInCorrectOrder()
    {
        var correlationId = Guid.NewGuid();
        var e0 = CreateSequencedEnvelope(correlationId, 0, 3, "first");
        var e1 = CreateSequencedEnvelope(correlationId, 1, 3, "second");
        var e2 = CreateSequencedEnvelope(correlationId, 2, 3, "third");

        // Arrive out of order: 2, 0, 1
        var r2 = _sut.Accept(e2);
        var r0 = _sut.Accept(e0);
        var r1 = _sut.Accept(e1);

        Assert.That(r2, Is.Empty);
        Assert.That(r0, Is.Empty);
        Assert.That(r1, Has.Count.EqualTo(3));

        // Verify ordering
        Assert.That(r1[0].SequenceNumber, Is.EqualTo(0));
        Assert.That(r1[1].SequenceNumber, Is.EqualTo(1));
        Assert.That(r1[2].SequenceNumber, Is.EqualTo(2));
    }

    [Test]
    public void Accept_SingleMessageSequence_ReleasesImmediately()
    {
        var correlationId = Guid.NewGuid();
        var e0 = CreateSequencedEnvelope(correlationId, 0, 1, "only");

        var result = _sut.Accept(e0);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Payload, Is.EqualTo("only"));
    }

    [Test]
    public void Accept_DuplicateSequenceNumber_IsIgnored()
    {
        var correlationId = Guid.NewGuid();
        var e0 = CreateSequencedEnvelope(correlationId, 0, 2, "first");
        var dup = CreateSequencedEnvelope(correlationId, 0, 2, "duplicate");
        var e1 = CreateSequencedEnvelope(correlationId, 1, 2, "second");

        _sut.Accept(e0);
        var dupResult = _sut.Accept(dup);
        var finalResult = _sut.Accept(e1);

        Assert.That(dupResult, Is.Empty, "duplicate ignored");
        Assert.That(finalResult, Has.Count.EqualTo(2));
        Assert.That(finalResult[0].Payload, Is.EqualTo("first"), "original kept, not duplicate");
    }

    [Test]
    public void Accept_NoSequenceInfo_ThrowsArgumentException()
    {
        var envelope = new IntegrationEnvelope<string>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Source = "Test",
            MessageType = "Order",
            Payload = "data",
        };

        Assert.Throws<ArgumentException>(() => _sut.Accept(envelope));
    }

    [Test]
    public void Accept_ZeroTotalCount_ThrowsArgumentException()
    {
        var envelope = CreateSequencedEnvelope(Guid.NewGuid(), 0, 0);
        // TotalCount = 0 is invalid, override
        var bad = envelope with { TotalCount = 0 };

        Assert.Throws<ArgumentException>(() => _sut.Accept(bad));
    }

    [Test]
    public void Accept_NullEnvelope_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Accept<string>(null!));
    }

    [Test]
    public void ReleaseOnTimeout_IncompleteSequence_ReleasesBufferedInOrder()
    {
        var correlationId = Guid.NewGuid();
        var e0 = CreateSequencedEnvelope(correlationId, 0, 5, "first");
        var e2 = CreateSequencedEnvelope(correlationId, 2, 5, "third");
        var e1 = CreateSequencedEnvelope(correlationId, 1, 5, "second");

        _sut.Accept(e0);
        _sut.Accept(e2);
        _sut.Accept(e1);

        // Only 3 of 5 arrived; force timeout release
        var released = _sut.ReleaseOnTimeout<string>(correlationId);

        Assert.That(released, Has.Count.EqualTo(3));
        Assert.That(released[0].SequenceNumber, Is.EqualTo(0));
        Assert.That(released[1].SequenceNumber, Is.EqualTo(1));
        Assert.That(released[2].SequenceNumber, Is.EqualTo(2));
    }

    [Test]
    public void ReleaseOnTimeout_UnknownCorrelationId_ReturnsEmpty()
    {
        var released = _sut.ReleaseOnTimeout<string>(Guid.NewGuid());
        Assert.That(released, Is.Empty);
    }

    [Test]
    public void ReleaseOnTimeout_ClearsBuffer()
    {
        var correlationId = Guid.NewGuid();
        _sut.Accept(CreateSequencedEnvelope(correlationId, 0, 3));

        Assert.That(_sut.ActiveSequenceCount, Is.EqualTo(1));

        _sut.ReleaseOnTimeout<string>(correlationId);

        Assert.That(_sut.ActiveSequenceCount, Is.EqualTo(0));
    }

    [Test]
    public void ActiveSequenceCount_TracksActiveBuffers()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        Assert.That(_sut.ActiveSequenceCount, Is.EqualTo(0));

        _sut.Accept(CreateSequencedEnvelope(id1, 0, 2));
        Assert.That(_sut.ActiveSequenceCount, Is.EqualTo(1));

        _sut.Accept(CreateSequencedEnvelope(id2, 0, 2));
        Assert.That(_sut.ActiveSequenceCount, Is.EqualTo(2));

        // Complete first sequence
        _sut.Accept(CreateSequencedEnvelope(id1, 1, 2));
        Assert.That(_sut.ActiveSequenceCount, Is.EqualTo(1));
    }

    [Test]
    public void Accept_MultipleSequences_IndependentBuffering()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        // Start two sequences interleaved
        _sut.Accept(CreateSequencedEnvelope(id1, 0, 2, "s1-first"));
        _sut.Accept(CreateSequencedEnvelope(id2, 1, 2, "s2-second"));
        _sut.Accept(CreateSequencedEnvelope(id2, 0, 2, "s2-first"));

        // Sequence 2 should be complete now
        // Get the result from the last accept
        var r = _sut.Accept(CreateSequencedEnvelope(id1, 1, 2, "s1-second"));

        Assert.That(r, Has.Count.EqualTo(2));
        Assert.That(r[0].Payload, Is.EqualTo("s1-first"));
        Assert.That(r[1].Payload, Is.EqualTo("s1-second"));
    }
}
