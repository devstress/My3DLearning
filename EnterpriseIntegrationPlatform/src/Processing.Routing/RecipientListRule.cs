namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Defines a rule that contributes destinations to the Recipient List.
/// </summary>
/// <remarks>
/// Rules are evaluated independently — ALL matching rules contribute their destinations.
/// </remarks>
public sealed record RecipientListRule
{
    /// <summary>
    /// The envelope field to evaluate. Supported values: <c>MessageType</c>,
    /// <c>Source</c>, <c>Priority</c>, <c>Metadata.{key}</c>.
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>The comparison operator to apply.</summary>
    public required RoutingOperator Operator { get; init; }

    /// <summary>The value to compare against the extracted field value.</summary>
    public required string Value { get; init; }

    /// <summary>
    /// The destinations to add when this rule matches.
    /// All destinations in this list are published to.
    /// </summary>
    public required IReadOnlyList<string> Destinations { get; init; }

    /// <summary>
    /// Optional human-readable name for this rule used in log output and diagnostics.
    /// </summary>
    public string? Name { get; init; }
}
