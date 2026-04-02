namespace EnterpriseIntegrationPlatform.Processing.ScatterGather;

/// <summary>
/// The aggregated result of a scatter-gather operation.
/// </summary>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
/// <param name="CorrelationId">The correlation identifier for this scatter-gather operation.</param>
/// <param name="Responses">The collected responses from recipients.</param>
/// <param name="TimedOut">
/// <see langword="true"/> when the gather phase was terminated by the timeout
/// before all recipients responded; <see langword="false"/> otherwise.
/// </param>
/// <param name="Duration">The total wall-clock time of the scatter-gather operation.</param>
public sealed record ScatterGatherResult<TResponse>(
    Guid CorrelationId,
    IReadOnlyList<GatherResponse<TResponse>> Responses,
    bool TimedOut,
    TimeSpan Duration);
