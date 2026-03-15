namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Defines a single content-based routing rule.
/// Rules are evaluated in ascending <see cref="Priority"/> order; the first matching rule wins.
/// </summary>
/// <remarks>
/// <para><strong>Supported field names:</strong></para>
/// <list type="bullet">
///   <item><description><c>MessageType</c> — the envelope's <c>MessageType</c> header.</description></item>
///   <item><description><c>Source</c> — the envelope's <c>Source</c> header.</description></item>
///   <item><description><c>Priority</c> — string representation of the envelope's <c>Priority</c> enum value.</description></item>
///   <item><description><c>Metadata.{key}</c> — a value from the envelope's <c>Metadata</c> dictionary, e.g. <c>Metadata.tenant</c>.</description></item>
///   <item><description><c>Payload.{path}</c> — a value from the JSON payload using dot-separated path notation, e.g. <c>Payload.order.status</c>. Only supported when the payload is a <c>JsonElement</c>.</description></item>
/// </list>
/// </remarks>
public sealed record RoutingRule
{
    /// <summary>
    /// Evaluation priority. Rules with lower values are evaluated first.
    /// When two rules have equal priority their order is unspecified.
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    /// The envelope field or payload path to evaluate.
    /// See the class remarks for supported values.
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>The comparison operator to apply.</summary>
    public required RoutingOperator Operator { get; init; }

    /// <summary>The value to compare against the extracted field value.</summary>
    public required string Value { get; init; }

    /// <summary>
    /// The message broker topic or subject to which matching messages are routed.
    /// </summary>
    public required string TargetTopic { get; init; }

    /// <summary>
    /// Optional human-readable name for this rule used in log output and diagnostics.
    /// </summary>
    public string? Name { get; init; }
}
