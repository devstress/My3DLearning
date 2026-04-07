using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for managing builder partners — registration, profile management, and quote request routing.
/// </summary>
public interface IBuilderService
{
    Task<BuilderProfile> RegisterAsync(Partner partner, BuilderProfile profile, CancellationToken cancellationToken = default);
    Task<BuilderProfile?> GetProfileAsync(Guid partnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuilderProfile>> FindBuildersAsync(int bedrooms, double floorArea, IReadOnlyList<string>? regions = null, CancellationToken cancellationToken = default);
    Task<PartnerQuoteResponse> RequestQuoteAsync(Guid partnerId, Guid quoteRequestId, HomeModel model, LandBlock block, CancellationToken cancellationToken = default);
    Task<PartnerQuoteResponse> SubmitQuoteResponseAsync(Guid partnerId, Guid quoteRequestId, decimal amountAud, int estimatedDays, string description, CancellationToken cancellationToken = default);
}
