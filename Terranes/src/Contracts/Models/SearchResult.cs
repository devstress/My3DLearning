namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a search result from the cross-entity search service.
/// </summary>
/// <param name="EntityType">Type of entity matched (e.g. "HomeModel", "VirtualVillage", "PropertyListing").</param>
/// <param name="EntityId">Identifier of the matched entity.</param>
/// <param name="Title">Display title of the matched entity.</param>
/// <param name="Summary">Short summary or description of the matched entity.</param>
/// <param name="Score">Relevance score (higher is better).</param>
public sealed record SearchResult(
    string EntityType,
    Guid EntityId,
    string Title,
    string Summary,
    double Score);
