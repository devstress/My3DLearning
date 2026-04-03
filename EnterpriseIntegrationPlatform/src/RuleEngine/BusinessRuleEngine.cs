using System.Text.Json;
using System.Text.RegularExpressions;
using EnterpriseIntegrationPlatform.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// Production implementation of the business rule evaluation engine.
/// </summary>
/// <remarks>
/// <para>
/// Rules are loaded from the <see cref="IRuleStore"/> and evaluated in ascending
/// <see cref="BusinessRule.Priority"/> order. Disabled rules are skipped.
/// </para>
/// <para>
/// Conditions within a rule are combined using the rule's <see cref="BusinessRule.LogicOperator"/>:
/// <see cref="RuleLogicOperator.And"/> requires all conditions to match;
/// <see cref="RuleLogicOperator.Or"/> requires at least one condition to match.
/// </para>
/// <para>
/// When a rule has <see cref="BusinessRule.StopOnMatch"/> set to <see langword="true"/>,
/// evaluation halts after that rule matches, returning its action.
/// When <see langword="false"/>, evaluation continues to collect additional matching rules.
/// </para>
/// </remarks>
public sealed class BusinessRuleEngine : IRuleEngine
{
    private readonly IRuleStore _ruleStore;
    private readonly RuleEngineOptions _options;
    private readonly ILogger<BusinessRuleEngine> _logger;
    private readonly TimeSpan _regexTimeout;

    /// <summary>Initialises a new instance of <see cref="BusinessRuleEngine"/>.</summary>
    public BusinessRuleEngine(
        IRuleStore ruleStore,
        IOptions<RuleEngineOptions> options,
        ILogger<BusinessRuleEngine> logger)
    {
        ArgumentNullException.ThrowIfNull(ruleStore);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _ruleStore = ruleStore;
        _options = options.Value;
        _logger = logger;
        _regexTimeout = _options.RegexTimeout > TimeSpan.Zero
            ? _options.RegexTimeout
            : TimeSpan.FromSeconds(5);
    }

    /// <inheritdoc />
    public async Task<RuleEvaluationResult> EvaluateAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!_options.Enabled)
        {
            _logger.LogDebug("Rule engine is disabled — skipping evaluation for message {MessageId}",
                envelope.MessageId);
            return new RuleEvaluationResult([], [], HasMatch: false, RulesEvaluated: 0);
        }

        var allRules = await _ruleStore.GetAllAsync(cancellationToken);
        var matchedRules = new List<BusinessRule>();
        var actions = new List<RuleAction>();
        var rulesEvaluated = 0;
        var maxRules = _options.MaxRulesPerEvaluation > 0
            ? _options.MaxRulesPerEvaluation
            : int.MaxValue;

        foreach (var rule in allRules)
        {
            if (rulesEvaluated >= maxRules)
            {
                _logger.LogDebug(
                    "Max rules per evaluation ({Max}) reached for message {MessageId}",
                    _options.MaxRulesPerEvaluation, envelope.MessageId);
                break;
            }

            if (!rule.Enabled)
                continue;

            rulesEvaluated++;

            if (EvaluateRule(envelope, rule))
            {
                matchedRules.Add(rule);
                actions.Add(rule.Action);

                _logger.LogDebug(
                    "Message {MessageId} (type={MessageType}) matched rule '{RuleName}' (priority={Priority}, action={Action})",
                    envelope.MessageId, envelope.MessageType, rule.Name, rule.Priority, rule.Action.ActionType);

                if (rule.StopOnMatch)
                    break;
            }
        }

        if (matchedRules.Count == 0)
        {
            _logger.LogDebug(
                "Message {MessageId} (type={MessageType}) matched no rules after evaluating {Count} rules",
                envelope.MessageId, envelope.MessageType, rulesEvaluated);
        }

        return new RuleEvaluationResult(matchedRules, actions, matchedRules.Count > 0, rulesEvaluated);
    }

    private bool EvaluateRule<T>(IntegrationEnvelope<T> envelope, BusinessRule rule)
    {
        if (rule.Conditions.Count == 0)
            return false;

        return rule.LogicOperator switch
        {
            RuleLogicOperator.And => rule.Conditions.All(c => EvaluateCondition(envelope, c)),
            RuleLogicOperator.Or => rule.Conditions.Any(c => EvaluateCondition(envelope, c)),
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

    /// <summary>
    /// Navigates a <see cref="JsonElement"/> using a dot-separated path and returns
    /// the string value of the terminal property, or <see langword="null"/> if any
    /// segment is missing.
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
