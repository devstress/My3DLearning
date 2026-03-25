using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.DeadLetter;

public sealed class DeadLetterPublisher<T> : IDeadLetterPublisher<T>
{
    private readonly IMessageBrokerProducer _producer;
    private readonly DeadLetterOptions _options;

    public DeadLetterPublisher(IMessageBrokerProducer producer, IOptions<DeadLetterOptions> options)
    {
        _producer = producer;
        _options = options.Value;
    }

    public async Task PublishAsync(
        IntegrationEnvelope<T> envelope,
        DeadLetterReason reason,
        string errorMessage,
        int attemptCount,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (string.IsNullOrWhiteSpace(_options.DeadLetterTopic))
            throw new InvalidOperationException("DeadLetterTopic must not be null or whitespace.");

        var deadLetterEnvelope = new DeadLetterEnvelope<T>
        {
            OriginalEnvelope = envelope,
            Reason = reason,
            ErrorMessage = errorMessage,
            FailedAt = DateTimeOffset.UtcNow,
            AttemptCount = attemptCount
        };

        var source = string.IsNullOrWhiteSpace(_options.Source) ? envelope.Source : _options.Source;
        var messageType = string.IsNullOrWhiteSpace(_options.MessageType) ? "DeadLetter" : _options.MessageType;

        var wrappedEnvelope = IntegrationEnvelope<DeadLetterEnvelope<T>>.Create(
            deadLetterEnvelope,
            source,
            messageType,
            correlationId: envelope.CorrelationId,
            causationId: envelope.MessageId);

        await _producer.PublishAsync(wrappedEnvelope, _options.DeadLetterTopic, ct);
    }
}
