namespace EnterpriseIntegrationPlatform.EventSourcing;

/// <summary>
/// Thrown when an append operation detects that the stream version does not match the expected version,
/// indicating a concurrent modification.
/// </summary>
public sealed class OptimisticConcurrencyException : InvalidOperationException
{
    /// <summary>Initialises a new instance of <see cref="OptimisticConcurrencyException"/>.</summary>
    /// <param name="streamId">The stream that was concurrently modified.</param>
    /// <param name="expectedVersion">The version the caller expected.</param>
    /// <param name="actualVersion">The actual current version of the stream.</param>
    public OptimisticConcurrencyException(string streamId, long expectedVersion, long actualVersion)
        : base($"Stream '{streamId}' expected version {expectedVersion} but was at {actualVersion}.")
    {
        StreamId = streamId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    /// <summary>The stream that was concurrently modified.</summary>
    public string StreamId { get; }

    /// <summary>The version the caller expected.</summary>
    public long ExpectedVersion { get; }

    /// <summary>The actual current version of the stream.</summary>
    public long ActualVersion { get; }
}
