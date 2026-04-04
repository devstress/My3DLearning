using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// Production implementation of the Test Message Enterprise Integration Pattern.
/// Generates synthetic test messages marked with metadata for downstream identification.
/// </summary>
public sealed class TestMessageGenerator : ITestMessageGenerator
{
    /// <summary>
    /// Metadata key added to test messages for downstream identification.
    /// </summary>
    public const string TestMessageMetadataKey = "eip-test-message";

    /// <summary>
    /// The message type used for standard (string-payload) test messages.
    /// </summary>
    public const string TestMessageType = "eip.system.test-message";

    private readonly IMessageBrokerProducer _producer;
    private readonly ILogger<TestMessageGenerator> _logger;

    /// <summary>Initialises a new instance of <see cref="TestMessageGenerator"/>.</summary>
    public TestMessageGenerator(
        IMessageBrokerProducer producer,
        ILogger<TestMessageGenerator> logger)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(logger);

        _producer = producer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TestMessageResult> GenerateAsync(
        string targetTopic,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetTopic);

        var testPayload = $"Test message generated at {DateTimeOffset.UtcNow:O}";
        return await GenerateAsync(testPayload, targetTopic, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TestMessageResult> GenerateAsync<T>(
        T payload,
        string targetTopic,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetTopic);

        var envelope = IntegrationEnvelope<T>.Create(
            payload,
            source: "TestMessageGenerator",
            messageType: TestMessageType);

        envelope.Metadata[TestMessageMetadataKey] = "true";
        envelope.Metadata["test-generated-at"] = DateTimeOffset.UtcNow.ToString("O");

        try
        {
            await _producer.PublishAsync(envelope, targetTopic, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Test message {MessageId} published to '{TargetTopic}'",
                envelope.MessageId, targetTopic);

            return new TestMessageResult(envelope.MessageId, targetTopic, Succeeded: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish test message to '{TargetTopic}'",
                targetTopic);

            return new TestMessageResult(envelope.MessageId, targetTopic, Succeeded: false,
                FailureReason: ex.Message);
        }
    }
}
