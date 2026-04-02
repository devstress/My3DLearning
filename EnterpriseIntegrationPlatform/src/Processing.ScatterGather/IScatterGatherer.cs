namespace EnterpriseIntegrationPlatform.Processing.ScatterGather;

/// <summary>
/// Implements the Scatter-Gather Enterprise Integration Pattern.
/// Broadcasts a request to multiple recipient topics, collects responses
/// within a configurable timeout window, and returns the aggregated result.
/// </summary>
/// <typeparam name="TRequest">The type of the request payload.</typeparam>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
public interface IScatterGatherer<TRequest, TResponse>
{
    /// <summary>
    /// Scatters <paramref name="request"/> to all configured recipients and gathers
    /// responses within the timeout window defined by <see cref="ScatterGatherOptions.TimeoutMs"/>.
    /// </summary>
    /// <param name="request">The scatter request containing payload and recipients.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ScatterGatherResult{TResponse}"/> containing all responses
    /// received before the timeout expired.
    /// </returns>
    Task<ScatterGatherResult<TResponse>> ScatterGatherAsync(
        ScatterRequest<TRequest> request,
        CancellationToken cancellationToken = default);
}
