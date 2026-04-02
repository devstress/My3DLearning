using System.Threading.Channels;

namespace EnterpriseIntegrationPlatform.Configuration;

/// <summary>
/// Pub/sub notification service for configuration changes using System.Threading.Channels.
/// Supports multiple concurrent subscribers, each receiving an independent stream of changes.
/// </summary>
public sealed class ConfigurationChangeNotifier : IObservable<ConfigurationChange>, IDisposable
{
    private readonly List<ChannelSubscription> _subscriptions = [];
    private readonly Lock _lock = new();
    private bool _disposed;

    /// <summary>
    /// Publishes a configuration change to all active subscribers.
    /// </summary>
    public void Publish(ConfigurationChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            // Remove completed subscriptions while iterating
            _subscriptions.RemoveAll(s => s.IsCompleted);

            foreach (var subscription in _subscriptions)
            {
                subscription.Writer.TryWrite(change);
            }
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(IObserver<ConfigurationChange> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var channel = Channel.CreateUnbounded<ConfigurationChange>(
            new UnboundedChannelOptions { SingleReader = true });

        var subscription = new ChannelSubscription(channel.Writer, this);

        lock (_lock)
        {
            _subscriptions.Add(subscription);
        }

        // Start pumping channel items to the observer on a background thread.
        _ = PumpAsync(channel.Reader, observer, subscription);

        return subscription;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Complete();
            }
            _subscriptions.Clear();
        }
    }

    private static async Task PumpAsync(
        ChannelReader<ConfigurationChange> reader,
        IObserver<ConfigurationChange> observer,
        ChannelSubscription subscription)
    {
        try
        {
            await foreach (var change in reader.ReadAllAsync())
            {
                observer.OnNext(change);
            }
            observer.OnCompleted();
        }
        catch (ChannelClosedException)
        {
            observer.OnCompleted();
        }
        catch (Exception ex)
        {
            observer.OnError(ex);
        }
        finally
        {
            subscription.MarkCompleted();
        }
    }

    private void Unsubscribe(ChannelSubscription subscription)
    {
        lock (_lock)
        {
            _subscriptions.Remove(subscription);
        }
        subscription.Complete();
    }

    private sealed class ChannelSubscription : IDisposable
    {
        private readonly ChannelWriter<ConfigurationChange> _writer;
        private readonly ConfigurationChangeNotifier _notifier;
        private int _completed;

        public ChannelSubscription(
            ChannelWriter<ConfigurationChange> writer,
            ConfigurationChangeNotifier notifier)
        {
            _writer = writer;
            _notifier = notifier;
        }

        public ChannelWriter<ConfigurationChange> Writer => _writer;
        public bool IsCompleted => Volatile.Read(ref _completed) == 1;

        public void MarkCompleted() => Interlocked.Exchange(ref _completed, 1);

        public void Complete()
        {
            if (Interlocked.Exchange(ref _completed, 1) == 0)
            {
                _writer.TryComplete();
            }
        }

        public void Dispose()
        {
            _notifier.Unsubscribe(this);
        }
    }
}
