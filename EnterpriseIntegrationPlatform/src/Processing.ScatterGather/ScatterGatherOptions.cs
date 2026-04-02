namespace EnterpriseIntegrationPlatform.Processing.ScatterGather;

/// <summary>
/// Configuration options for <see cref="ScatterGatherer{TRequest,TResponse}"/>.
/// Bind from the <c>ScatterGather</c> configuration section.
/// </summary>
public sealed class ScatterGatherOptions
{
    /// <summary>
    /// Maximum time in milliseconds to wait for all recipient responses before
    /// the gather phase completes with a partial result. Defaults to 30 000 ms.
    /// </summary>
    public int TimeoutMs { get; set; } = 30_000;

    /// <summary>
    /// Maximum number of recipients allowed in a single scatter request.
    /// Requests exceeding this limit are rejected. Defaults to 50.
    /// </summary>
    public int MaxRecipients { get; set; } = 50;
}
