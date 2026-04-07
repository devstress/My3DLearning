using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for real estate agent integration — listings sync, agent management, and MLS feeds.
/// </summary>
public interface IRealEstateAgentService
{
    Task<AgentProfile> RegisterAsync(Partner partner, AgentProfile profile, CancellationToken cancellationToken = default);
    Task<AgentProfile?> GetProfileAsync(Guid partnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentProfile>> FindAgentsAsync(string? suburb = null, bool? acceptsSelfListings = null, IReadOnlyList<string>? regions = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PropertyListing>> GetAgentListingsAsync(Guid partnerId, CancellationToken cancellationToken = default);
    Task<PropertyListing> SyncListingAsync(Guid partnerId, PropertyListing listing, CancellationToken cancellationToken = default);
}
