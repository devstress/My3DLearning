using System.Text.Json;
using System.Text.RegularExpressions;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Production implementation of the Recipient List EIP pattern.
/// </summary>
/// <remarks>
/// <para>
/// Resolves a list of target destinations for each message and publishes the unmodified
/// envelope to ALL resolved recipients (fan-out). Destinations are resolved from:
/// </para>
/// <list type="number">
///   <item><description>Rule-based resolution: ALL matching <see cref="RecipientListRule"/>
///   rules contribute their <see cref="RecipientListRule.Destinations"/>.</description></item>
///   <item><description>Metadata-based resolution: if <see cref="RecipientListOptions.MetadataRecipientsKey"/>
///   is configured, the comma-separated value from the envelope metadata is parsed as
///   additional destinations.</description></item>
/// </list>
/// <para>
/// Duplicate destinations are removed (case-insensitive). Publishing is done concurrently
/// to all resolved destinations.
/// </para>
/// </remarks>
public sealed class RecipientListRouter : IRecipientList
{
    private readonly IMessageBrokerProducer _producer;
    private readonly RecipientListOptions _options;
    private readonly ILogger<RecipientListRouter> _logger;
    private readonly IReadOnlyDictionary<RecipientListRule, Regex> _compiledRegexes;

    /// <summary>Initialises a new instance of <see cref="RecipientListRouter"/>.</summary>
    public RecipientListRouter(
        IMessageBrokerProducer producer,
        IOptions<RecipientListOptions> options,
        ILogger<RecipientListRouter> logger)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _producer = producer;
        _options = options.Value;
        _logger = logger;
        _compiledRegexes = _options.Rules
            .Where(r => r.Operator == RoutingOperator.Regex)
            .ToDictionary(
                r => r,
                r => new Regex(r.Value, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
                    matchTimeout: TimeSpan.FromSeconds(1)));
    }

    /// <inheritdoc />
    public async Task<RecipientListResult> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var allDestinations = new List<string>();

        // 1. Rule-based resolution
        foreach (var rule in _options.Rules)
        {
            var fieldValue = ExtractFieldValue(envelope, rule.FieldName);
            if (fieldValue is not null && Matches(fieldValue, rule))
            {
                allDestinations.AddRange(rule.Destinations);
            }
        }

        // 2. Metadata-based resolution
        if (!string.IsNullOrWhiteSpace(_options.MetadataRecipientsKey) &&
            envelope.Metadata.TryGetValue(_options.MetadataRecipientsKey, out var metaRecipients) &&
            !string.IsNullOrWhiteSpace(metaRecipients))
        {
            var metaDestinations = metaRecipients
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            allDestinations.AddRange(metaDestinations);
        }

        // 3. Deduplicate (case-insensitive)
        var totalBeforeDedup = allDestinations.Count;
        var uniqueDestinations = allDestinations
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var duplicatesRemoved = totalBeforeDedup - uniqueDestinations.Count;

        // 4. Publish to all destinations concurrently
        if (uniqueDestinations.Count > 0)
        {
            var publishTasks = uniqueDestinations
                .Select(dest => _producer.PublishAsync(envelope, dest, cancellationToken));
            await Task.WhenAll(publishTasks);
        }

        _logger.LogDebug(
            "Message {MessageId} published to {Count} recipients (dedup removed {Duplicates}): [{Destinations}]",
            envelope.MessageId, uniqueDestinations.Count, duplicatesRemoved,
            string.Join(", ", uniqueDestinations));

        return new RecipientListResult(
            Destinations: uniqueDestinations.AsReadOnly(),
            ResolvedCount: uniqueDestinations.Count,
            DuplicatesRemoved: duplicatesRemoved);
    }

    // ------------------------------------------------------------------ //
    // Field extraction — consistent with ContentBasedRouter / DynamicRouter
    // ------------------------------------------------------------------ //

    private static string? ExtractFieldValue<T>(IntegrationEnvelope<T> envelope, string fieldName)
    {
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

    private bool Matches(string fieldValue, RecipientListRule rule) =>
        rule.Operator switch
        {
            RoutingOperator.Equals =>
                string.Equals(fieldValue, rule.Value, StringComparison.OrdinalIgnoreCase),

            RoutingOperator.Contains =>
                fieldValue.Contains(rule.Value, StringComparison.OrdinalIgnoreCase),

            RoutingOperator.StartsWith =>
                fieldValue.StartsWith(rule.Value, StringComparison.OrdinalIgnoreCase),

            RoutingOperator.Regex =>
                _compiledRegexes.TryGetValue(rule, out var rx)
                    ? rx.IsMatch(fieldValue)
                    : Regex.IsMatch(fieldValue, rule.Value, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),

            _ => false,
        };
}
