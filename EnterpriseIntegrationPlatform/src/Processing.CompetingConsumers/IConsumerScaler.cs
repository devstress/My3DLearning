namespace EnterpriseIntegrationPlatform.Processing.CompetingConsumers;

/// <summary>
/// Scales the number of logical consumer instances.
/// </summary>
public interface IConsumerScaler
{
    /// <summary>
    /// Gets the current number of active consumer instances.
    /// </summary>
    int CurrentCount { get; }

    /// <summary>
    /// Scales the consumer pool to the desired number of instances.
    /// </summary>
    /// <param name="desiredCount">The target number of consumer instances.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task ScaleAsync(int desiredCount, CancellationToken cancellationToken);
}
