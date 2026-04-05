using System.Text.Json;
using System.Text.RegularExpressions;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using EnterpriseIntegrationPlatform.RuleEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Production implementation of the Message Filter EIP pattern.
/// </summary>
/// <remarks>
/// <para>
/// Evaluates <see cref="RuleCondition"/> predicates (reused from the RuleEngine)
/// against an envelope. Messages that pass are published to the configured output topic;
/// messages that fail are either silently discarded or routed to a configurable discard
/// topic (e.g. a Dead Letter Queue).
/// </para>
/// <para>
/// Multiple conditions can be combined with AND or OR logic via
/// <see cref="MessageFilterOptions.Logic"/>.
/// </para>
/// </remarks>
public sealed class MessageFilter : IMessageFilter
{
    private readonly IMessageBrokerProducer _producer;
    private readonly MessageFilterOptions _options;
    private readonly ILogger<MessageFilter> _logger;
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Initialises a new instance of <see cref="MessageFilter"/>.</summary>
    public MessageFilter(
        IMessageBrokerProducer producer,
        IOptions<MessageFilterOptions> options,
        ILogger<MessageFilter> logger)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MessageFilterResult> FilterAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var passed = EvaluatePredicate(envelope);

        if (passed)
        {
            await _producer.PublishAsync(envelope, _options.OutputTopic, cancellationToken);

            _logger.LogDebug(
                "Message {MessageId} (type={MessageType}) passed filter — published to '{Topic}'",
                envelope.MessageId, envelope.MessageType, _options.OutputTopic);

            return new MessageFilterResult(
                Passed: true,
                OutputTopic: _options.OutputTopic,
                Reason: "Predicate matched");
        }

        // Discarded
        if (!string.IsNullOrWhiteSpace(_options.DiscardTopic))
        {
            // If the DLQ publish fails, the exception propagates so the caller can Nack.
            await _producer.PublishAsync(envelope, _options.DiscardTopic, cancellationToken);

            _logger.LogDebug(
                "Message {MessageId} (type={MessageType}) failed filter — routed to discard topic '{Topic}'",
                envelope.MessageId, envelope.MessageType, _options.DiscardTopic);

            return new MessageFilterResult(
                Passed: false,
                OutputTopic: _options.DiscardTopic,
                Reason: "Predicate did not match — routed to discard topic");
        }

        // No discard topic configured — enforce no-silent-drop when required.
        if (_options.RequireDiscardTopic)
        {
            throw new InvalidOperationException(
                $"Message {envelope.MessageId} (type={envelope.MessageType}) failed the filter " +
                "predicate, but no DiscardTopic is configured and RequireDiscardTopic is true. " +
                "Configure a DiscardTopic to prevent silent message loss.");
        }

        _logger.LogDebug(
            "Message {MessageId} (type={MessageType}) failed filter — silently discarded",
            envelope.MessageId, envelope.MessageType);

        return new MessageFilterResult(
            Passed: false,
            OutputTopic: null,
            Reason: "Predicate did not match — silently discarded");
    }

    // ------------------------------------------------------------------ //
    // Predicate evaluation
    // ------------------------------------------------------------------ //

    private bool EvaluatePredicate<T>(IntegrationEnvelope<T> envelope)
    {
        if (_options.Conditions.Count == 0)
            return true; // No conditions = pass-through

        return _options.Logic switch
        {
            RuleLogicOperator.And => _options.Conditions.All(c => EvaluateCondition(envelope, c)),
            RuleLogicOperator.Or => _options.Conditions.Any(c => EvaluateCondition(envelope, c)),
            _ => false,
        };
    }

    private bool EvaluateCondition<T>(IntegrationEnvelope<T> envelope, RuleCondition condition)
    {
        var fieldValue = ExtractFieldValue(envelope, condition.FieldName);
        if (fieldValue is null)
            return false;

        return condition.Operator switch
        {
            RuleConditionOperator.Equals =>
                string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),

            RuleConditionOperator.Contains =>
                fieldValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),

            RuleConditionOperator.Regex =>
                EvaluateRegex(fieldValue, condition.Value),

            RuleConditionOperator.In =>
                EvaluateIn(fieldValue, condition.Value),

            RuleConditionOperator.GreaterThan =>
                EvaluateGreaterThan(fieldValue, condition.Value),

            _ => false,
        };
    }

    private bool EvaluateRegex(string fieldValue, string pattern)
    {
        try
        {
            return Regex.IsMatch(
                fieldValue,
                pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                _regexTimeout);
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogWarning(ex,
                "Regex evaluation timed out for pattern '{Pattern}' against value '{Value}'",
                pattern, fieldValue);
            return false;
        }
    }

    private static bool EvaluateIn(string fieldValue, string commaSeparatedValues)
    {
        var values = commaSeparatedValues.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return values.Any(v => string.Equals(fieldValue, v, StringComparison.OrdinalIgnoreCase));
    }

    private static bool EvaluateGreaterThan(string fieldValue, string thresholdValue)
    {
        if (decimal.TryParse(fieldValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fieldDecimal) &&
            decimal.TryParse(thresholdValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var thresholdDecimal))
        {
            return fieldDecimal > thresholdDecimal;
        }

        return false;
    }

    // ------------------------------------------------------------------ //
    // Field extraction — consistent with RuleEngine and other routers
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
}
