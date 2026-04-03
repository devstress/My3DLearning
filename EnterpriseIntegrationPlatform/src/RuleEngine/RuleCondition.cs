namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// A single condition within a <see cref="BusinessRule"/>.
/// </summary>
/// <remarks>
/// <para><strong>Supported field names:</strong></para>
/// <list type="bullet">
///   <item><description><c>MessageType</c> — the envelope's <c>MessageType</c> header.</description></item>
///   <item><description><c>Source</c> — the envelope's <c>Source</c> header.</description></item>
///   <item><description><c>Priority</c> — string representation of the envelope's <c>Priority</c> enum value.</description></item>
///   <item><description><c>Metadata.{key}</c> — a value from the envelope's <c>Metadata</c> dictionary.</description></item>
///   <item><description><c>Payload.{path}</c> — a value from the JSON payload using dot-separated path notation. Only supported when the payload is a <c>JsonElement</c>.</description></item>
/// </list>
/// </remarks>
public sealed record RuleCondition
{
    /// <summary>
    /// The envelope field or payload path to evaluate.
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>The comparison operator to apply.</summary>
    public required RuleConditionOperator Operator { get; init; }

    /// <summary>
    /// The value to compare against the extracted field value.
    /// For <see cref="RuleConditionOperator.In"/>, use a comma-separated list.
    /// </summary>
    public required string Value { get; init; }
}
