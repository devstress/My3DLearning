using System.Collections.Concurrent;
using System.Text.Json;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Production implementation of the Dynamic Router EIP pattern.
/// </summary>
/// <remarks>
/// <para>
/// Unlike the <see cref="ContentBasedRouter"/> whose rules are configured statically at
/// startup, the Dynamic Router maintains an internal routing table that is updated at
/// runtime by downstream participants via <see cref="IRouterControlChannel"/>.
/// Participants register/unregister conditions and destinations via control messages.
/// </para>
/// <para>
/// The condition field (e.g. <c>MessageType</c>) is extracted from each envelope and
/// looked up in the routing table. If a match is found, the message is published to the
/// registered destination. If no match is found, the configurable
/// <see cref="DynamicRouterOptions.FallbackTopic"/> is used. If no fallback is configured,
/// an <see cref="InvalidOperationException"/> is thrown.
/// </para>
/// <para>
/// Thread-safety: the routing table uses <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// for lock-free concurrent reads and writes.
/// </para>
/// </remarks>
public sealed class DynamicRouter : IDynamicRouter, IRouterControlChannel
{
    private readonly IMessageBrokerProducer _producer;
    private readonly DynamicRouterOptions _options;
    private readonly ILogger<DynamicRouter> _logger;
    private readonly ConcurrentDictionary<string, DynamicRouteEntry> _routingTable;
    private readonly StringComparer _keyComparer;

    /// <summary>Initialises a new instance of <see cref="DynamicRouter"/>.</summary>
    public DynamicRouter(
        IMessageBrokerProducer producer,
        IOptions<DynamicRouterOptions> options,
        ILogger<DynamicRouter> logger)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _producer = producer;
        _options = options.Value;
        _logger = logger;
        _keyComparer = _options.CaseInsensitive
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        _routingTable = new ConcurrentDictionary<string, DynamicRouteEntry>(_keyComparer);
    }

    // ------------------------------------------------------------------ //
    // IDynamicRouter
    // ------------------------------------------------------------------ //

    /// <inheritdoc />
    public async Task<DynamicRoutingDecision> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var conditionValue = ExtractConditionValue(envelope);

        if (conditionValue is not null &&
            _routingTable.TryGetValue(conditionValue, out var entry))
        {
            await _producer.PublishAsync(envelope, entry.Destination, cancellationToken);

            _logger.LogDebug(
                "Message {MessageId} (condition={ConditionValue}) routed to dynamic destination '{Destination}' " +
                "(participant={ParticipantId})",
                envelope.MessageId, conditionValue, entry.Destination, entry.ParticipantId ?? "(none)");

            return new DynamicRoutingDecision(
                Destination: entry.Destination,
                MatchedEntry: entry,
                IsFallback: false,
                ConditionValue: conditionValue);
        }

        // No match — fallback.
        if (!string.IsNullOrWhiteSpace(_options.FallbackTopic))
        {
            await _producer.PublishAsync(envelope, _options.FallbackTopic, cancellationToken);

            _logger.LogDebug(
                "Message {MessageId} (condition={ConditionValue}) matched no dynamic route — " +
                "routed to fallback topic '{Topic}'",
                envelope.MessageId, conditionValue ?? "(null)", _options.FallbackTopic);

            return new DynamicRoutingDecision(
                Destination: _options.FallbackTopic,
                MatchedEntry: null,
                IsFallback: true,
                ConditionValue: conditionValue);
        }

        throw new InvalidOperationException(
            $"No dynamic route matched message {envelope.MessageId} " +
            $"(conditionField='{_options.ConditionField}', conditionValue='{conditionValue ?? "(null)"}') " +
            "and no FallbackTopic is configured.");
    }

    // ------------------------------------------------------------------ //
    // IRouterControlChannel
    // ------------------------------------------------------------------ //

    /// <inheritdoc />
    public Task RegisterAsync(
        string conditionKey,
        string destination,
        string? participantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        var entry = new DynamicRouteEntry(
            ConditionKey: conditionKey,
            Destination: destination,
            ParticipantId: participantId,
            RegisteredAtUtc: DateTimeOffset.UtcNow);

        _routingTable.AddOrUpdate(conditionKey, entry, (_, _) => entry);

        _logger.LogInformation(
            "Dynamic route registered: condition='{ConditionKey}' → destination='{Destination}' " +
            "(participant={ParticipantId})",
            conditionKey, destination, participantId ?? "(none)");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> UnregisterAsync(
        string conditionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionKey);

        var removed = _routingTable.TryRemove(conditionKey, out var entry);

        if (removed)
        {
            _logger.LogInformation(
                "Dynamic route unregistered: condition='{ConditionKey}' (was → '{Destination}', participant={ParticipantId})",
                conditionKey, entry!.Destination, entry.ParticipantId ?? "(none)");
        }
        else
        {
            _logger.LogDebug(
                "Dynamic route unregister ignored — no entry for condition='{ConditionKey}'",
                conditionKey);
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, DynamicRouteEntry> GetRoutingTable() =>
        _routingTable.ToDictionary(_keyComparer);

    // ------------------------------------------------------------------ //
    // Field extraction — reuses the same approach as ContentBasedRouter
    // ------------------------------------------------------------------ //

    private string? ExtractConditionValue<T>(IntegrationEnvelope<T> envelope)
    {
        var fieldName = _options.ConditionField;

        if (string.Equals(fieldName, "MessageType", StringComparison.OrdinalIgnoreCase))
            return envelope.MessageType;

        if (string.Equals(fieldName, "Source", StringComparison.OrdinalIgnoreCase))
            return envelope.Source;

        if (string.Equals(fieldName, "Priority", StringComparison.OrdinalIgnoreCase))
            return envelope.Priority.ToString();

        if (fieldName.StartsWith("Metadata.", StringComparison.OrdinalIgnoreCase))
        {
            var key = fieldName["Metadata.".Length..];
            return envelope.Metadata.TryGetValue(key, out var metaValue) ? metaValue : null;
        }

        if (fieldName.StartsWith("Payload.", StringComparison.OrdinalIgnoreCase))
        {
            var path = fieldName["Payload.".Length..];
            if (envelope.Payload is JsonElement jsonElement)
                return ExtractJsonValue(jsonElement, path);

            return null;
        }

        return null;
    }

    /// <summary>
    /// Navigates a <see cref="JsonElement"/> using a dot-separated path and returns
    /// the string value of the terminal property.
    /// </summary>
    private static string? ExtractJsonValue(JsonElement root, string path)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var current = root;

        foreach (var segment in segments)
        {
            if (current.ValueKind != JsonValueKind.Object)
                return null;

            if (!current.TryGetProperty(segment, out current))
                return null;
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => current.GetRawText(),
        };
    }
}
