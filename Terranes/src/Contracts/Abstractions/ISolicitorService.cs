using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for solicitor partner integration — lawyer matching, contract templates, and conveyancing.
/// </summary>
public interface ISolicitorService
{
    Task<SolicitorProfile> RegisterAsync(Partner partner, SolicitorProfile profile, CancellationToken cancellationToken = default);
    Task<SolicitorProfile?> GetProfileAsync(Guid partnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SolicitorProfile>> FindSolicitorsAsync(bool? conveyancing = null, bool? contractReview = null, IReadOnlyList<string>? regions = null, CancellationToken cancellationToken = default);
    Task<PartnerQuoteResponse> RequestQuoteAsync(Guid partnerId, Guid quoteRequestId, string serviceType, CancellationToken cancellationToken = default);
}
