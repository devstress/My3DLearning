namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Describes the outcome of a Recipient List routing evaluation.
/// </summary>
/// <param name="Destinations">
/// The list of destinations to which the message was published.
/// Empty if no recipients were resolved.
/// </param>
/// <param name="ResolvedCount">The total number of unique destinations resolved.</param>
/// <param name="DuplicatesRemoved">
/// The number of duplicate destinations that were removed before publishing.
/// </param>
public sealed record RecipientListResult(
    IReadOnlyList<string> Destinations,
    int ResolvedCount,
    int DuplicatesRemoved);
