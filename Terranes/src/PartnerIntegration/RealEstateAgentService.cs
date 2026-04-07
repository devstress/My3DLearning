using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.PartnerIntegration;

/// <summary>
/// In-memory implementation of <see cref="IRealEstateAgentService"/>.
/// Manages agent registration, listings sync, and MLS feed integration.
/// </summary>
public sealed class RealEstateAgentService : IRealEstateAgentService
{
    private readonly ConcurrentDictionary<Guid, Partner> _partners = new();
    private readonly ConcurrentDictionary<Guid, AgentProfile> _profiles = new();
    private readonly ConcurrentDictionary<Guid, List<PropertyListing>> _agentListings = new();
    private readonly IMarketplaceService _marketplaceService;
    private readonly ILogger<RealEstateAgentService> _logger;

    public RealEstateAgentService(IMarketplaceService marketplaceService, ILogger<RealEstateAgentService> logger)
    {
        _marketplaceService = marketplaceService;
        _logger = logger;
    }

    public Task<AgentProfile> RegisterAsync(Partner partner, AgentProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(partner);
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(partner.BusinessName))
            throw new ArgumentException("Business name is required.", nameof(partner));

        if (partner.Category != PartnerCategory.RealEstateAgent)
            throw new ArgumentException("Partner must be in the RealEstateAgent category.", nameof(partner));

        if (string.IsNullOrWhiteSpace(profile.LicenseNumber))
            throw new ArgumentException("License number is required.", nameof(profile));

        if (profile.CommissionPercentage < 0 || profile.CommissionPercentage > 100)
            throw new ArgumentException("Commission percentage must be between 0 and 100.", nameof(profile));

        if (profile.CoverageSuburbs.Count == 0)
            throw new ArgumentException("At least one coverage suburb is required.", nameof(profile));

        var registeredPartner = partner with { Id = partner.Id == Guid.Empty ? Guid.NewGuid() : partner.Id, IsActive = true, RegisteredAtUtc = DateTimeOffset.UtcNow };
        var registeredProfile = profile with { PartnerId = registeredPartner.Id };

        if (!_partners.TryAdd(registeredPartner.Id, registeredPartner))
            throw new InvalidOperationException($"Agent partner {registeredPartner.Id} already exists.");

        _profiles[registeredPartner.Id] = registeredProfile;
        _agentListings[registeredPartner.Id] = [];

        _logger.LogInformation("Registered real estate agent {PartnerId} (License: {License})", registeredPartner.Id, registeredProfile.LicenseNumber);
        return Task.FromResult(registeredProfile);
    }

    public Task<AgentProfile?> GetProfileAsync(Guid partnerId, CancellationToken cancellationToken = default)
    {
        _profiles.TryGetValue(partnerId, out var profile);
        return Task.FromResult(profile);
    }

    public Task<IReadOnlyList<AgentProfile>> FindAgentsAsync(string? suburb = null, bool? acceptsSelfListings = null, IReadOnlyList<string>? regions = null, CancellationToken cancellationToken = default)
    {
        var query = _profiles.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(suburb))
            query = query.Where(p => p.CoverageSuburbs.Any(s => s.Equals(suburb, StringComparison.OrdinalIgnoreCase)));

        if (acceptsSelfListings.HasValue)
            query = query.Where(p => p.AcceptsSelfListings == acceptsSelfListings.Value);

        if (regions is { Count: > 0 })
        {
            var regionSet = new HashSet<string>(regions, StringComparer.OrdinalIgnoreCase);
            query = query.Where(p => _partners.TryGetValue(p.PartnerId, out var partner) &&
                partner.ServiceRegions.Any(r => regionSet.Contains(r)));
        }

        query = query.Where(p => _partners.TryGetValue(p.PartnerId, out var partner) && partner.IsActive);

        IReadOnlyList<AgentProfile> result = query.OrderByDescending(p => p.ActiveListingsCount).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<PropertyListing>> GetAgentListingsAsync(Guid partnerId, CancellationToken cancellationToken = default)
    {
        if (!_partners.ContainsKey(partnerId))
            throw new InvalidOperationException($"Agent partner {partnerId} not found.");

        IReadOnlyList<PropertyListing> listings = _agentListings.TryGetValue(partnerId, out var list)
            ? list.AsReadOnly()
            : [];
        return Task.FromResult(listings);
    }

    public async Task<PropertyListing> SyncListingAsync(Guid partnerId, PropertyListing listing, CancellationToken cancellationToken = default)
    {
        if (!_partners.ContainsKey(partnerId))
            throw new InvalidOperationException($"Agent partner {partnerId} not found.");

        ArgumentNullException.ThrowIfNull(listing);

        // Create the listing in the marketplace
        var created = await _marketplaceService.CreateListingAsync(listing with { ListedByUserId = partnerId }, cancellationToken);

        // Track it in the agent's listing collection
        _agentListings.AddOrUpdate(partnerId, _ => [created], (_, list) => { list.Add(created); return list; });

        // Update active listings count
        if (_profiles.TryGetValue(partnerId, out var profile))
        {
            _profiles[partnerId] = profile with { ActiveListingsCount = _agentListings[partnerId].Count };
        }

        _logger.LogInformation("Agent {PartnerId} synced listing {ListingId}", partnerId, created.Id);
        return created;
    }
}
