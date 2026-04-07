using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for managing virtual 3D villages — neighbourhood scenes with multiple lots.
/// </summary>
public interface IVirtualVillageService
{
    /// <summary>Creates a new virtual village with the specified layout.</summary>
    Task<VirtualVillage> CreateAsync(VirtualVillage village, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a village by its unique identifier.</summary>
    Task<VirtualVillage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Searches villages by name or layout type.</summary>
    Task<IReadOnlyList<VirtualVillage>> SearchAsync(string? name = null, VillageLayoutType? layout = null, CancellationToken cancellationToken = default);

    /// <summary>Adds a lot to a village.</summary>
    Task<VillageLot> AddLotAsync(VillageLot lot, CancellationToken cancellationToken = default);

    /// <summary>Gets all lots for a village.</summary>
    Task<IReadOnlyList<VillageLot>> GetLotsAsync(Guid villageId, CancellationToken cancellationToken = default);

    /// <summary>Assigns a site placement to a lot.</summary>
    Task<VillageLot> AssignPlacementAsync(Guid lotId, Guid sitePlacementId, CancellationToken cancellationToken = default);

    /// <summary>Gets village summary statistics.</summary>
    Task<(int TotalLots, int OccupiedLots, int VacantLots)> GetStatsAsync(Guid villageId, CancellationToken cancellationToken = default);
}
