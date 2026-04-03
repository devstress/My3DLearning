using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Ingestion.Channels;

/// <summary>
/// Invalid Message Channel — routes unparseable or invalid-schema messages to a
/// dedicated invalid-message topic. Distinct from Dead Letter Queue: DLQ is for
/// processing failures on well-formed messages; Invalid Message Channel is for
/// malformed input that cannot be parsed or fails schema validation at ingestion.
/// </summary>
public interface IInvalidMessageChannel
{
    /// <summary>
    /// Routes an invalid message to the invalid-message channel.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The original envelope (may have a partially parsed payload).</param>
    /// <param name="reason">Human-readable reason why the message is invalid.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RouteInvalidAsync<T>(
        IntegrationEnvelope<T> envelope,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Routes raw invalid data that could not be deserialized into an envelope.
    /// </summary>
    /// <param name="rawData">The raw message bytes or string that failed parsing.</param>
    /// <param name="sourceTopic">The topic from which the invalid data was received.</param>
    /// <param name="reason">Human-readable reason why the message is invalid.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RouteRawInvalidAsync(
        string rawData,
        string sourceTopic,
        string reason,
        CancellationToken cancellationToken = default);
}
