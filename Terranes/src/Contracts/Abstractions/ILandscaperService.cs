using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for managing landscaper partners — registration, design templates, and quote routing.
/// </summary>
public interface ILandscaperService
{
    Task<LandscaperProfile> RegisterAsync(Partner partner, LandscaperProfile profile, CancellationToken cancellationToken = default);
    Task<LandscaperProfile?> GetProfileAsync(Guid partnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LandscaperProfile>> FindLandscapersAsync(LandscapeStyle? style = null, double? minArea = null, IReadOnlyList<string>? regions = null, CancellationToken cancellationToken = default);
    Task<LandscapeDesign> CreateDesignAsync(LandscapeDesign design, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LandscapeDesign>> GetDesignsForPlacementAsync(Guid sitePlacementId, CancellationToken cancellationToken = default);
    Task<PartnerQuoteResponse> RequestQuoteAsync(Guid partnerId, Guid quoteRequestId, Terranes.Contracts.Models.SitePlacement placement, LandscapeStyle style, CancellationToken cancellationToken = default);
}
