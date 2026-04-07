using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.PartnerIntegration;

/// <summary>
/// In-memory implementation of <see cref="ISolicitorService"/>.
/// Manages solicitor registration, matching, and quote routing.
/// </summary>
public sealed class SolicitorService : ISolicitorService
{
    private readonly ConcurrentDictionary<Guid, Partner> _partners = new();
    private readonly ConcurrentDictionary<Guid, SolicitorProfile> _profiles = new();
    private readonly ConcurrentDictionary<string, PartnerQuoteResponse> _quoteResponses = new();
    private readonly ILogger<SolicitorService> _logger;

    public SolicitorService(ILogger<SolicitorService> logger) => _logger = logger;

    public Task<SolicitorProfile> RegisterAsync(Partner partner, SolicitorProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(partner);
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(partner.BusinessName))
            throw new ArgumentException("Business name is required.", nameof(partner));

        if (partner.Category != PartnerCategory.Solicitor)
            throw new ArgumentException("Partner must be in the Solicitor category.", nameof(partner));

        if (profile.FixedFeeAud < 0)
            throw new ArgumentException("Fixed fee cannot be negative.", nameof(profile));

        if (profile.YearsExperience < 0)
            throw new ArgumentException("Years of experience cannot be negative.", nameof(profile));

        if (!profile.OffersConveyancing && !profile.OffersContractReview)
            throw new ArgumentException("Solicitor must offer at least one service (conveyancing or contract review).", nameof(profile));

        var registeredPartner = partner with { Id = partner.Id == Guid.Empty ? Guid.NewGuid() : partner.Id, IsActive = true, RegisteredAtUtc = DateTimeOffset.UtcNow };
        var registeredProfile = profile with { PartnerId = registeredPartner.Id };

        if (!_partners.TryAdd(registeredPartner.Id, registeredPartner))
            throw new InvalidOperationException($"Solicitor partner {registeredPartner.Id} already exists.");

        _profiles[registeredPartner.Id] = registeredProfile;

        _logger.LogInformation("Registered solicitor partner {PartnerId}", registeredPartner.Id);
        return Task.FromResult(registeredProfile);
    }

    public Task<SolicitorProfile?> GetProfileAsync(Guid partnerId, CancellationToken cancellationToken = default)
    {
        _profiles.TryGetValue(partnerId, out var profile);
        return Task.FromResult(profile);
    }

    public Task<IReadOnlyList<SolicitorProfile>> FindSolicitorsAsync(bool? conveyancing = null, bool? contractReview = null, IReadOnlyList<string>? regions = null, CancellationToken cancellationToken = default)
    {
        var query = _profiles.Values.AsEnumerable();

        if (conveyancing == true)
            query = query.Where(p => p.OffersConveyancing);

        if (contractReview == true)
            query = query.Where(p => p.OffersContractReview);

        if (regions is { Count: > 0 })
        {
            var regionSet = new HashSet<string>(regions, StringComparer.OrdinalIgnoreCase);
            query = query.Where(p => _partners.TryGetValue(p.PartnerId, out var partner) &&
                partner.ServiceRegions.Any(r => regionSet.Contains(r)));
        }

        query = query.Where(p => _partners.TryGetValue(p.PartnerId, out var partner) && partner.IsActive);

        IReadOnlyList<SolicitorProfile> result = query.OrderBy(p => p.FixedFeeAud).ToList();
        return Task.FromResult(result);
    }

    public Task<PartnerQuoteResponse> RequestQuoteAsync(Guid partnerId, Guid quoteRequestId, string serviceType, CancellationToken cancellationToken = default)
    {
        if (!_partners.ContainsKey(partnerId))
            throw new InvalidOperationException($"Solicitor partner {partnerId} not found.");

        if (string.IsNullOrWhiteSpace(serviceType))
            throw new ArgumentException("Service type is required.", nameof(serviceType));

        var profile = _profiles[partnerId];
        var fee = profile.FixedFeeAud;

        var response = new PartnerQuoteResponse(
            Guid.NewGuid(), quoteRequestId, partnerId, PartnerCategory.Solicitor,
            PartnerQuoteStatus.Quoted, fee, $"{serviceType} — fixed fee",
            null, DateTimeOffset.UtcNow.AddDays(14), DateTimeOffset.UtcNow);

        _quoteResponses[$"{partnerId}:{quoteRequestId}"] = response;

        _logger.LogInformation("Solicitor {PartnerId} quoted ${Fee} for {QuoteRequestId}", partnerId, fee, quoteRequestId);
        return Task.FromResult(response);
    }
}
