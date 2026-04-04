using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Production implementation of the Detour Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// <para>
/// Conditionally routes messages through a validation/debug/test pipeline
/// before normal processing. The detour can be toggled globally via
/// <see cref="SetEnabled"/> or per-message via a metadata key.
/// </para>
/// <para>
/// Thread-safe: the global enabled flag uses <c>volatile</c> for lock-free reads.
/// </para>
/// </remarks>
public sealed class Detour : IDetour
{
    private readonly IMessageBrokerProducer _producer;
    private readonly DetourOptions _options;
    private readonly ILogger<Detour> _logger;
    private volatile bool _enabled;

    /// <summary>Initialises a new instance of <see cref="Detour"/>.</summary>
    public Detour(
        IMessageBrokerProducer producer,
        IOptions<DetourOptions> options,
        ILogger<Detour> logger)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _producer = producer;
        _options = options.Value;
        _logger = logger;
        _enabled = _options.EnabledAtStartup;
    }

    /// <inheritdoc />
    public bool IsEnabled => _enabled;

    /// <inheritdoc />
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        _logger.LogInformation("Detour globally {State}", enabled ? "enabled" : "disabled");
    }

    /// <inheritdoc />
    public async Task<DetourResult> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var shouldDetour = _enabled || IsPerMessageDetour(envelope);
        var targetTopic = shouldDetour ? _options.DetourTopic : _options.OutputTopic;
        var reason = shouldDetour
            ? (_enabled
                ? "Global detour is enabled"
                : $"Per-message detour activated via metadata key '{_options.DetourMetadataKey}'")
            : "Detour inactive — normal routing";

        await _producer.PublishAsync(envelope, targetTopic, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Message {MessageId} routed to '{TargetTopic}' (detoured={Detoured})",
            envelope.MessageId, targetTopic, shouldDetour);

        return new DetourResult(shouldDetour, targetTopic, reason);
    }

    private bool IsPerMessageDetour<T>(IntegrationEnvelope<T> envelope)
    {
        if (string.IsNullOrWhiteSpace(_options.DetourMetadataKey))
            return false;

        return envelope.Metadata.TryGetValue(_options.DetourMetadataKey, out var value) &&
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
