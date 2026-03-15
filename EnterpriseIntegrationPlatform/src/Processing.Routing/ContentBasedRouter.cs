using System.Text.Json;
using System.Text.RegularExpressions;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Production implementation of the Content-Based Router EIP pattern.
/// </summary>
/// <remarks>
/// <para>
/// Rules are evaluated in ascending <see cref="RoutingRule.Priority"/> order.
/// The first matching rule determines the target topic. When no rule matches, the
/// <see cref="RouterOptions.DefaultTopic"/> is used. When no default is configured
/// and no rule matches, an <see cref="InvalidOperationException"/> is thrown.
/// </para>
/// <para>
/// The matched (or default) topic is used to publish the original unmodified envelope
/// via the registered <see cref="IMessageBrokerProducer"/>.
/// </para>
/// </remarks>
public sealed class ContentBasedRouter : IContentBasedRouter
{
    private readonly IMessageBrokerProducer _producer;
    private readonly RouterOptions _options;
    private readonly ILogger<ContentBasedRouter> _logger;

    // Pre-sorted rule list and pre-compiled Regex instances built once at construction time.
    private readonly IReadOnlyList<RoutingRule> _sortedRules;
    private readonly IReadOnlyDictionary<RoutingRule, Regex> _compiledRegexes;

    /// <summary>Initialises a new instance of <see cref="ContentBasedRouter"/>.</summary>
    public ContentBasedRouter(
        IMessageBrokerProducer producer,
        IOptions<RouterOptions> options,
        ILogger<ContentBasedRouter> logger)
    {
        _producer = producer;
        _options = options.Value;
        _logger = logger;
        _sortedRules = [.. _options.Rules.OrderBy(r => r.Priority)];
        _compiledRegexes = _sortedRules
            .Where(r => r.Operator == RoutingOperator.Regex)
            .ToDictionary(
                r => r,
                r => new Regex(r.Value, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
                    matchTimeout: TimeSpan.FromSeconds(1)));
    }

    /// <inheritdoc />
    public async Task<RoutingDecision> RouteAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var decision = Evaluate(envelope);

        await _producer.PublishAsync(envelope, decision.TargetTopic, cancellationToken);

        if (decision.IsDefault)
        {
            _logger.LogDebug(
                "Message {MessageId} (type={MessageType}) matched no rule — routed to default topic '{Topic}'",
                envelope.MessageId, envelope.MessageType, decision.TargetTopic);
        }
        else
        {
            _logger.LogDebug(
                "Message {MessageId} (type={MessageType}) matched rule '{RuleName}' (priority={Priority}) — routed to '{Topic}'",
                envelope.MessageId, envelope.MessageType,
                decision.MatchedRule!.Name ?? decision.MatchedRule.FieldName,
                decision.MatchedRule.Priority,
                decision.TargetTopic);
        }

        return decision;
    }

    private RoutingDecision Evaluate<T>(IntegrationEnvelope<T> envelope)
    {
        foreach (var rule in _sortedRules)
        {
            var fieldValue = ExtractFieldValue(envelope, rule.FieldName);
            if (fieldValue is not null && Matches(fieldValue, rule))
            {
                return new RoutingDecision(
                    TargetTopic: rule.TargetTopic,
                    MatchedRule: rule,
                    IsDefault: false);
            }
        }

        // No rule matched — use default topic.
        if (!string.IsNullOrWhiteSpace(_options.DefaultTopic))
        {
            return new RoutingDecision(
                TargetTopic: _options.DefaultTopic,
                MatchedRule: null,
                IsDefault: true);
        }

        throw new InvalidOperationException(
            $"No routing rule matched message {envelope.MessageId} (type='{envelope.MessageType}') " +
            "and no DefaultTopic is configured.");
    }

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

            // Non-JsonElement payloads cannot be inspected by path.
            return null;
        }

        return null;
    }

    /// <summary>
    /// Navigates a <see cref="JsonElement"/> using a dot-separated path and returns
    /// the string value of the terminal property, or <see langword="null"/> if any
    /// segment is missing or cannot be represented as a string.
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

    private bool Matches(string fieldValue, RoutingRule rule) =>
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
