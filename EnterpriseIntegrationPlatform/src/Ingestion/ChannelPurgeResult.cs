namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Describes the outcome of a <see cref="IChannelPurger.PurgeAsync"/> call.
/// </summary>
/// <param name="Topic">The topic that was purged.</param>
/// <param name="PurgedCount">The number of messages that were drained and discarded.</param>
/// <param name="Succeeded">Whether the purge operation completed successfully.</param>
/// <param name="FailureReason">Description of the failure, if any.</param>
public sealed record ChannelPurgeResult(
    string Topic,
    int PurgedCount,
    bool Succeeded,
    string? FailureReason = null);
