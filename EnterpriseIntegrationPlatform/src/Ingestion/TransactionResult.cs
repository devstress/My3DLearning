namespace EnterpriseIntegrationPlatform.Ingestion;

/// <summary>
/// Result of a transactional client operation.
/// </summary>
public sealed record TransactionResult
{
    /// <summary>Whether the transaction was committed successfully.</summary>
    public required bool Committed { get; init; }

    /// <summary>Number of messages published within the transaction.</summary>
    public required int MessageCount { get; init; }

    /// <summary>Error message when the transaction was rolled back.</summary>
    public string? Error { get; init; }

    /// <summary>The exception that caused the rollback, if any.</summary>
    public Exception? Exception { get; init; }

    /// <summary>Duration of the transaction.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Creates a successful transaction result.</summary>
    public static TransactionResult Success(int messageCount, TimeSpan duration) =>
        new()
        {
            Committed = true,
            MessageCount = messageCount,
            Duration = duration,
        };

    /// <summary>Creates a failed transaction result.</summary>
    public static TransactionResult Failure(string error, Exception? exception = null, TimeSpan duration = default) =>
        new()
        {
            Committed = false,
            MessageCount = 0,
            Error = error,
            Exception = exception,
            Duration = duration,
        };
}
