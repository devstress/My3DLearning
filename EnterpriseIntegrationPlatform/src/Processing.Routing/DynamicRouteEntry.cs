namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Describes a single entry in the Dynamic Router's routing table.
/// </summary>
/// <param name="ConditionKey">
/// The value that identifies which messages should be routed to <paramref name="Destination"/>.
/// Matched against the configured <see cref="DynamicRouterOptions.ConditionField"/> on the envelope.
/// </param>
/// <param name="Destination">The target topic or subject to which matching messages are published.</param>
/// <param name="ParticipantId">
/// Optional identifier of the downstream participant that registered this entry.
/// Used for diagnostics and auditing.
/// </param>
/// <param name="RegisteredAtUtc">UTC timestamp when this entry was registered or last updated.</param>
public sealed record DynamicRouteEntry(
    string ConditionKey,
    string Destination,
    string? ParticipantId,
    DateTimeOffset RegisteredAtUtc);
