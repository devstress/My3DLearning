using System.Collections.Concurrent;

namespace EnterpriseIntegrationPlatform.Processing.CompetingConsumers;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IConsumerLagMonitor"/>
/// that supports reporting and querying consumer lag.
/// </summary>
public sealed class InMemoryConsumerLagMonitor : IConsumerLagMonitor
{
    private readonly ConcurrentDictionary<string, ConsumerLagInfo> _lagData = new();

    /// <summary>
    /// Reports a lag measurement for a specific topic and consumer group.
    /// </summary>
    /// <param name="lagInfo">The lag information to report.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task ReportLagAsync(ConsumerLagInfo lagInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lagInfo);
        cancellationToken.ThrowIfCancellationRequested();

        var key = BuildKey(lagInfo.Topic, lagInfo.ConsumerGroup);
        _lagData[key] = lagInfo;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ConsumerLagInfo> GetLagAsync(string topic, string consumerGroup, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);
        cancellationToken.ThrowIfCancellationRequested();

        var key = BuildKey(topic, consumerGroup);
        if (_lagData.TryGetValue(key, out var lagInfo))
        {
            return Task.FromResult(lagInfo);
        }

        return Task.FromResult(new ConsumerLagInfo(consumerGroup, topic, 0, DateTimeOffset.UtcNow));
    }

    private static string BuildKey(string topic, string consumerGroup) =>
        $"{topic}::{consumerGroup}";
}
