using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for performing building regulation compliance checks per jurisdiction.
/// </summary>
public interface IComplianceService
{
    Task<ComplianceResult> CheckAsync(Guid sitePlacementId, string jurisdiction, CancellationToken cancellationToken = default);
    Task<ComplianceResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplianceResult>> GetBySitePlacementAsync(Guid sitePlacementId, CancellationToken cancellationToken = default);
}
