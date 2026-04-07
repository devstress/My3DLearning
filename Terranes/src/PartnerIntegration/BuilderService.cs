using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.PartnerIntegration;

/// <summary>
/// In-memory implementation of <see cref="IBuilderService"/>.
/// Manages builder partner registration, profile lookup, matching, and quote request/response.
/// </summary>
public sealed class BuilderService : IBuilderService
{
    private readonly ConcurrentDictionary<Guid, Partner> _partners = new();
    private readonly ConcurrentDictionary<Guid, BuilderProfile> _profiles = new();
    private readonly ConcurrentDictionary<string, PartnerQuoteResponse> _quoteResponses = new();
    private readonly ILogger<BuilderService> _logger;

    public BuilderService(ILogger<BuilderService> logger) => _logger = logger;

    public Task<BuilderProfile> RegisterAsync(Partner partner, BuilderProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(partner);
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(partner.BusinessName))
            throw new ArgumentException("Business name is required.", nameof(partner));

        if (partner.Category != PartnerCategory.Builder)
            throw new ArgumentException("Partner must be in the Builder category.", nameof(partner));

        if (profile.MinBuildPriceAud < 0 || profile.MaxBuildPriceAud < profile.MinBuildPriceAud)
            throw new ArgumentException("Build price range is invalid.", nameof(profile));

        if (profile.MinBedrooms < 0 || profile.MaxBedrooms < profile.MinBedrooms)
            throw new ArgumentException("Bedroom range is invalid.", nameof(profile));

        if (profile.MaxFloorAreaSquareMetres <= 0)
            throw new ArgumentException("Max floor area must be positive.", nameof(profile));

        var registeredPartner = partner with { Id = partner.Id == Guid.Empty ? Guid.NewGuid() : partner.Id, IsActive = true, RegisteredAtUtc = DateTimeOffset.UtcNow };
        var registeredProfile = profile with { PartnerId = registeredPartner.Id };

        if (!_partners.TryAdd(registeredPartner.Id, registeredPartner))
            throw new InvalidOperationException($"Builder partner {registeredPartner.Id} already exists.");

        _profiles[registeredPartner.Id] = registeredProfile;

        _logger.LogInformation("Registered builder partner {PartnerId} ({BuilderType})", registeredPartner.Id, registeredProfile.BuilderType);
        return Task.FromResult(registeredProfile);
    }

    public Task<BuilderProfile?> GetProfileAsync(Guid partnerId, CancellationToken cancellationToken = default)
    {
        _profiles.TryGetValue(partnerId, out var profile);
        return Task.FromResult(profile);
    }

    public Task<IReadOnlyList<BuilderProfile>> FindBuildersAsync(int bedrooms, double floorArea, IReadOnlyList<string>? regions = null, CancellationToken cancellationToken = default)
    {
        var query = _profiles.Values.Where(p =>
            p.MinBedrooms <= bedrooms && p.MaxBedrooms >= bedrooms &&
            p.MaxFloorAreaSquareMetres >= floorArea);

        if (regions is { Count: > 0 })
        {
            var regionSet = new HashSet<string>(regions, StringComparer.OrdinalIgnoreCase);
            query = query.Where(p => _partners.TryGetValue(p.PartnerId, out var partner) &&
                partner.ServiceRegions.Any(r => regionSet.Contains(r)));
        }

        // Only return active partners
        query = query.Where(p => _partners.TryGetValue(p.PartnerId, out var partner) && partner.IsActive);

        IReadOnlyList<BuilderProfile> result = query.ToList();
        return Task.FromResult(result);
    }

    public Task<PartnerQuoteResponse> RequestQuoteAsync(Guid partnerId, Guid quoteRequestId, HomeModel model, LandBlock block, CancellationToken cancellationToken = default)
    {
        if (!_partners.ContainsKey(partnerId))
            throw new InvalidOperationException($"Builder partner {partnerId} not found.");

        if (!_profiles.ContainsKey(partnerId))
            throw new InvalidOperationException($"Builder profile for {partnerId} not found.");

        var response = new PartnerQuoteResponse(
            Guid.NewGuid(), quoteRequestId, partnerId, PartnerCategory.Builder,
            PartnerQuoteStatus.Requested, null, null, null, null, DateTimeOffset.UtcNow);

        _quoteResponses[QuoteKey(partnerId, quoteRequestId)] = response;

        _logger.LogInformation("Sent quote request to builder {PartnerId} for quote {QuoteRequestId}", partnerId, quoteRequestId);
        return Task.FromResult(response);
    }

    public Task<PartnerQuoteResponse> SubmitQuoteResponseAsync(Guid partnerId, Guid quoteRequestId, decimal amountAud, int estimatedDays, string description, CancellationToken cancellationToken = default)
    {
        var key = QuoteKey(partnerId, quoteRequestId);
        if (!_quoteResponses.TryGetValue(key, out var existing))
            throw new InvalidOperationException($"No quote request found for builder {partnerId} / quote {quoteRequestId}.");

        if (existing.Status != PartnerQuoteStatus.Requested)
            throw new InvalidOperationException($"Cannot submit response for quote in status {existing.Status}.");

        if (amountAud < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amountAud));

        if (estimatedDays <= 0)
            throw new ArgumentException("Estimated days must be positive.", nameof(estimatedDays));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        var updated = existing with
        {
            Status = PartnerQuoteStatus.Quoted,
            AmountAud = amountAud,
            EstimatedDays = estimatedDays,
            Description = description,
            ValidUntilUtc = DateTimeOffset.UtcNow.AddDays(30),
            RespondedAtUtc = DateTimeOffset.UtcNow
        };

        _quoteResponses[key] = updated;

        _logger.LogInformation("Builder {PartnerId} submitted quote ${Amount} for {QuoteRequestId}", partnerId, amountAud, quoteRequestId);
        return Task.FromResult(updated);
    }

    private static string QuoteKey(Guid partnerId, Guid quoteRequestId) => $"{partnerId}:{quoteRequestId}";
}
