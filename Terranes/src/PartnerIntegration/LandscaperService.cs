using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.PartnerIntegration;

/// <summary>
/// In-memory implementation of <see cref="ILandscaperService"/>.
/// Manages landscaper registration, design templates, and quote routing.
/// </summary>
public sealed class LandscaperService : ILandscaperService
{
    private readonly ConcurrentDictionary<Guid, Partner> _partners = new();
    private readonly ConcurrentDictionary<Guid, LandscaperProfile> _profiles = new();
    private readonly ConcurrentDictionary<Guid, LandscapeDesign> _designs = new();
    private readonly ConcurrentDictionary<string, PartnerQuoteResponse> _quoteResponses = new();
    private readonly ILogger<LandscaperService> _logger;

    public LandscaperService(ILogger<LandscaperService> logger) => _logger = logger;

    public Task<LandscaperProfile> RegisterAsync(Partner partner, LandscaperProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(partner);
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(partner.BusinessName))
            throw new ArgumentException("Business name is required.", nameof(partner));

        if (partner.Category != PartnerCategory.Landscaper)
            throw new ArgumentException("Partner must be in the Landscaper category.", nameof(partner));

        if (profile.MaxAreaSquareMetres <= 0)
            throw new ArgumentException("Max area must be positive.", nameof(profile));

        if (profile.MinPriceAud < 0 || profile.MaxPriceAud < profile.MinPriceAud)
            throw new ArgumentException("Price range is invalid.", nameof(profile));

        if (profile.SupportedStyles.Count == 0)
            throw new ArgumentException("At least one supported style is required.", nameof(profile));

        var registeredPartner = partner with { Id = partner.Id == Guid.Empty ? Guid.NewGuid() : partner.Id, IsActive = true, RegisteredAtUtc = DateTimeOffset.UtcNow };
        var registeredProfile = profile with { PartnerId = registeredPartner.Id };

        if (!_partners.TryAdd(registeredPartner.Id, registeredPartner))
            throw new InvalidOperationException($"Landscaper partner {registeredPartner.Id} already exists.");

        _profiles[registeredPartner.Id] = registeredProfile;

        _logger.LogInformation("Registered landscaper partner {PartnerId}", registeredPartner.Id);
        return Task.FromResult(registeredProfile);
    }

    public Task<LandscaperProfile?> GetProfileAsync(Guid partnerId, CancellationToken cancellationToken = default)
    {
        _profiles.TryGetValue(partnerId, out var profile);
        return Task.FromResult(profile);
    }

    public Task<IReadOnlyList<LandscaperProfile>> FindLandscapersAsync(LandscapeStyle? style = null, double? minArea = null, IReadOnlyList<string>? regions = null, CancellationToken cancellationToken = default)
    {
        var query = _profiles.Values.AsEnumerable();

        if (style.HasValue)
            query = query.Where(p => p.SupportedStyles.Contains(style.Value));

        if (minArea.HasValue)
            query = query.Where(p => p.MaxAreaSquareMetres >= minArea.Value);

        if (regions is { Count: > 0 })
        {
            var regionSet = new HashSet<string>(regions, StringComparer.OrdinalIgnoreCase);
            query = query.Where(p => _partners.TryGetValue(p.PartnerId, out var partner) &&
                partner.ServiceRegions.Any(r => regionSet.Contains(r)));
        }

        query = query.Where(p => _partners.TryGetValue(p.PartnerId, out var partner) && partner.IsActive);

        IReadOnlyList<LandscaperProfile> result = query.ToList();
        return Task.FromResult(result);
    }

    public Task<LandscapeDesign> CreateDesignAsync(LandscapeDesign design, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(design);

        if (string.IsNullOrWhiteSpace(design.TemplateName))
            throw new ArgumentException("Template name is required.", nameof(design));

        if (design.EstimatedCostAud < 0)
            throw new ArgumentException("Estimated cost cannot be negative.", nameof(design));

        if (design.CoverageAreaSquareMetres <= 0)
            throw new ArgumentException("Coverage area must be positive.", nameof(design));

        var persisted = design with { Id = design.Id == Guid.Empty ? Guid.NewGuid() : design.Id, CreatedAtUtc = DateTimeOffset.UtcNow };

        if (!_designs.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Landscape design {persisted.Id} already exists.");

        _logger.LogInformation("Created landscape design {DesignId} for placement {PlacementId}", persisted.Id, persisted.SitePlacementId);
        return Task.FromResult(persisted);
    }

    public Task<IReadOnlyList<LandscapeDesign>> GetDesignsForPlacementAsync(Guid sitePlacementId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LandscapeDesign> results = _designs.Values
            .Where(d => d.SitePlacementId == sitePlacementId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToList();
        return Task.FromResult(results);
    }

    public Task<PartnerQuoteResponse> RequestQuoteAsync(Guid partnerId, Guid quoteRequestId, Terranes.Contracts.Models.SitePlacement placement, LandscapeStyle style, CancellationToken cancellationToken = default)
    {
        if (!_partners.ContainsKey(partnerId))
            throw new InvalidOperationException($"Landscaper partner {partnerId} not found.");

        var response = new PartnerQuoteResponse(
            Guid.NewGuid(), quoteRequestId, partnerId, PartnerCategory.Landscaper,
            PartnerQuoteStatus.Requested, null, null, null, null, DateTimeOffset.UtcNow);

        _quoteResponses[$"{partnerId}:{quoteRequestId}"] = response;

        _logger.LogInformation("Sent quote request to landscaper {PartnerId} for quote {QuoteRequestId}", partnerId, quoteRequestId);
        return Task.FromResult(response);
    }
}
