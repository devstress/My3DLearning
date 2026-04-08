using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace UnitTests;

/// <summary>
/// Tests for <see cref="BrokerTransactionalClient"/> — the Transactional Client EIP pattern.
/// Covers commit success, rollback on failure, timeout, non-transactional broker fallback,
/// empty transaction, compensation, and constructor validation.
/// </summary>
[TestFixture]
public sealed class TransactionalClientTests
{
    private IMessageBrokerProducer _producer = null!;
    private ILogger<BrokerTransactionalClient> _logger = null!;
    private IOptions<BrokerOptions> _kafkaOptions = null!;
    private IOptions<BrokerOptions> _natsOptions = null!;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IMessageBrokerProducer>();
        _logger = Substitute.For<ILogger<BrokerTransactionalClient>>();

        _kafkaOptions = Options.Create(new BrokerOptions
        {
            BrokerType = BrokerType.Kafka,
            TransactionTimeoutSeconds = 5,
        });

        _natsOptions = Options.Create(new BrokerOptions
        {
            BrokerType = BrokerType.NatsJetStream,
            TransactionTimeoutSeconds = 5,
        });
    }

    // ── Commit Success ──────────────────────────────────────────────────────

    [Test]
    public async Task ExecuteAsync_SuccessfulPublish_ReturnsCommitted()
    {
        var sut = new BrokerTransactionalClient(_producer, _kafkaOptions, _logger);
        var envelope = CreateEnvelope("test-payload");

        var result = await sut.ExecuteAsync(async (scope, ct) =>
        {
            await scope.PublishAsync(envelope, "orders.created", ct);
        });

        Assert.That(result.Committed, Is.True);
        Assert.That(result.MessageCount, Is.EqualTo(1));
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task ExecuteAsync_MultiplePublishes_CommitsAll()
    {
        var sut = new BrokerTransactionalClient(_producer, _natsOptions, _logger);
        var envelope1 = CreateEnvelope("payload-1");
        var envelope2 = CreateEnvelope("payload-2");
        var envelope3 = CreateEnvelope("payload-3");

        var result = await sut.ExecuteAsync(async (scope, ct) =>
        {
            await scope.PublishAsync(envelope1, "topic-a", ct);
            await scope.PublishAsync(envelope2, "topic-b", ct);
            await scope.PublishAsync(envelope3, "topic-c", ct);
        });

        Assert.That(result.Committed, Is.True);
        Assert.That(result.MessageCount, Is.EqualTo(3));
    }

    // ── Rollback on Failure ─────────────────────────────────────────────────

    [Test]
    public async Task ExecuteAsync_OperationThrows_ReturnsFailure()
    {
        var sut = new BrokerTransactionalClient(_producer, _kafkaOptions, _logger);

        var result = await sut.ExecuteAsync(async (scope, ct) =>
        {
            var envelope = CreateEnvelope("will-fail");
            await scope.PublishAsync(envelope, "orders.created", ct);
            throw new InvalidOperationException("Business logic error");
        });

        Assert.That(result.Committed, Is.False);
        Assert.That(result.Error, Does.Contain("Business logic error"));
        Assert.That(result.Exception, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task ExecuteAsync_ProducerThrows_ReturnsFailure()
    {
        _producer.PublishAsync(Arg.Any<IntegrationEnvelope<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Broker unavailable"));

        var sut = new BrokerTransactionalClient(_producer, _natsOptions, _logger);
        var envelope = CreateEnvelope("will-fail");

        var result = await sut.ExecuteAsync(async (scope, ct) =>
        {
            await scope.PublishAsync(envelope, "orders.created", ct);
        });

        Assert.That(result.Committed, Is.False);
        Assert.That(result.Error, Does.Contain("Broker unavailable"));
    }

    // ── Rollback publishes compensation ─────────────────────────────────────

    [Test]
    public async Task ExecuteAsync_RollbackAfterPublish_PublishesCompensation()
    {
        // First call succeeds, second call throws
        var callCount = 0;
        _producer.PublishAsync(Arg.Any<IntegrationEnvelope<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount == 2)
                {
                    throw new InvalidOperationException("Second publish failed");
                }
                return Task.CompletedTask;
            });

        var sut = new BrokerTransactionalClient(_producer, _natsOptions, _logger);
        var envelope1 = CreateEnvelope("msg-1");
        var envelope2 = CreateEnvelope("msg-2");

        var result = await sut.ExecuteAsync(async (scope, ct) =>
        {
            await scope.PublishAsync(envelope1, "topic-a", ct);
            await scope.PublishAsync(envelope2, "topic-b", ct); // This throws
        });

        Assert.That(result.Committed, Is.False);

        // Verify compensation was attempted — producer receives the original + compensation calls
        await _producer.Received().PublishAsync(
            Arg.Is<IntegrationEnvelope<string>>(e => e.MessageType == "system.transaction.compensate"),
            Arg.Is<string>(t => t == "topic-a.dlq"),
            Arg.Any<CancellationToken>());
    }

    // ── Timeout ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ExecuteAsync_Timeout_ReturnsFailureWithTimeoutError()
    {
        var shortTimeoutOptions = Options.Create(new BrokerOptions
        {
            BrokerType = BrokerType.Kafka,
            TransactionTimeoutSeconds = 1,
        });

        var sut = new BrokerTransactionalClient(_producer, shortTimeoutOptions, _logger);

        var result = await sut.ExecuteAsync(async (scope, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct); // Exceed timeout
        });

        Assert.That(result.Committed, Is.False);
        Assert.That(result.Error, Does.Contain("timed out"));
    }

    // ── Non-transactional broker fallback ───────────────────────────────────

    [Test]
    public void SupportsNativeTransactions_Kafka_ReturnsTrue()
    {
        var sut = new BrokerTransactionalClient(_producer, _kafkaOptions, _logger);

        Assert.That(sut.SupportsNativeTransactions, Is.True);
    }

    [Test]
    public void SupportsNativeTransactions_Nats_ReturnsFalse()
    {
        var sut = new BrokerTransactionalClient(_producer, _natsOptions, _logger);

        Assert.That(sut.SupportsNativeTransactions, Is.False);
    }

    [Test]
    public void SupportsNativeTransactions_Pulsar_ReturnsFalse()
    {
        var pulsarOptions = Options.Create(new BrokerOptions
        {
            BrokerType = BrokerType.Pulsar,
            TransactionTimeoutSeconds = 5,
        });

        var sut = new BrokerTransactionalClient(_producer, pulsarOptions, _logger);

        Assert.That(sut.SupportsNativeTransactions, Is.False);
    }

    // ── Empty Transaction ───────────────────────────────────────────────────

    [Test]
    public async Task ExecuteAsync_NoPublishes_ReturnsCommittedWithZeroMessages()
    {
        var sut = new BrokerTransactionalClient(_producer, _kafkaOptions, _logger);

        var result = await sut.ExecuteAsync((_, _) => Task.CompletedTask);

        Assert.That(result.Committed, Is.True);
        Assert.That(result.MessageCount, Is.EqualTo(0));
    }

    // ── Duration Tracking ───────────────────────────────────────────────────

    [Test]
    public async Task ExecuteAsync_TracksDuration()
    {
        var sut = new BrokerTransactionalClient(_producer, _kafkaOptions, _logger);
        var envelope = CreateEnvelope("test-payload");

        var result = await sut.ExecuteAsync(async (scope, ct) =>
        {
            await scope.PublishAsync(envelope, "topic", ct);
        });

        Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
    }

    // ── Constructor Validation ──────────────────────────────────────────────

    [Test]
    public void Constructor_NullProducer_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BrokerTransactionalClient(null!, _kafkaOptions, _logger));
    }

    [Test]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BrokerTransactionalClient(_producer, null!, _logger));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BrokerTransactionalClient(_producer, _kafkaOptions, null!));
    }

    // ── Null operations delegate ────────────────────────────────────────────

    [Test]
    public void ExecuteAsync_NullOperations_ThrowsArgumentNullException()
    {
        var sut = new BrokerTransactionalClient(_producer, _kafkaOptions, _logger);

        Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.ExecuteAsync(null!));
    }

    // ── TransactionResult factory tests ─────────────────────────────────────

    [Test]
    public void TransactionResult_Success_SetsPropertiesCorrectly()
    {
        var duration = TimeSpan.FromMilliseconds(150);
        var result = TransactionResult.Success(5, duration);

        Assert.That(result.Committed, Is.True);
        Assert.That(result.MessageCount, Is.EqualTo(5));
        Assert.That(result.Duration, Is.EqualTo(duration));
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Exception, Is.Null);
    }

    [Test]
    public void TransactionResult_Failure_SetsPropertiesCorrectly()
    {
        var ex = new InvalidOperationException("test");
        var result = TransactionResult.Failure("something failed", ex);

        Assert.That(result.Committed, Is.False);
        Assert.That(result.MessageCount, Is.EqualTo(0));
        Assert.That(result.Error, Is.EqualTo("something failed"));
        Assert.That(result.Exception, Is.SameAs(ex));
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private static IntegrationEnvelope<string> CreateEnvelope(string payload) =>
        IntegrationEnvelope<string>.Create(payload, "TestService", "test.message");
}
