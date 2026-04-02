namespace EnterpriseIntegrationPlatform.Processing.Throttle;

/// <summary>
/// Outcome of a throttle acquisition attempt.
/// </summary>
public sealed record ThrottleResult
{
    /// <summary>Whether a token was acquired and processing may proceed.</summary>
    public required bool Permitted { get; init; }

    /// <summary>Time the caller waited to acquire a token.</summary>
    public required TimeSpan WaitTime { get; init; }

    /// <summary>Remaining tokens in the bucket after acquisition.</summary>
    public required int RemainingTokens { get; init; }

    /// <summary>
    /// When <see cref="Permitted"/> is <c>false</c>, describes the reason
    /// (e.g. backpressure, timeout, cancellation).
    /// </summary>
    public string? RejectionReason { get; init; }
}
