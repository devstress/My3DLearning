using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Immersive3D;

/// <summary>
/// In-memory implementation of <see cref="IVirtualVillageService"/>.
/// Manages virtual village creation, lot allocation, and home placement.
/// </summary>
public sealed class VirtualVillageService : IVirtualVillageService
{
    private readonly ConcurrentDictionary<Guid, VirtualVillage> _villages = new();
    private readonly ConcurrentDictionary<Guid, VillageLot> _lots = new();
    private readonly ILogger<VirtualVillageService> _logger;

    public VirtualVillageService(ILogger<VirtualVillageService> logger) => _logger = logger;

    public Task<VirtualVillage> CreateAsync(VirtualVillage village, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(village);

        if (string.IsNullOrWhiteSpace(village.Name))
            throw new ArgumentException("Village name is required.", nameof(village));

        if (village.MaxLots <= 0)
            throw new ArgumentException("MaxLots must be positive.", nameof(village));

        if (village.MaxLots > 500)
            throw new ArgumentException("MaxLots cannot exceed 500.", nameof(village));

        var persisted = village with { Id = village.Id == Guid.Empty ? Guid.NewGuid() : village.Id, CreatedAtUtc = DateTimeOffset.UtcNow };

        if (!_villages.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Village with ID {persisted.Id} already exists.");

        _logger.LogInformation("Created virtual village {VillageId} ({Layout}, {MaxLots} lots)", persisted.Id, persisted.Layout, persisted.MaxLots);
        return Task.FromResult(persisted);
    }

    public Task<VirtualVillage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _villages.TryGetValue(id, out var village);
        return Task.FromResult(village);
    }

    public Task<IReadOnlyList<VirtualVillage>> SearchAsync(string? name = null, VillageLayoutType? layout = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<VirtualVillage> query = _villages.Values;

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(v => v.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (layout.HasValue)
            query = query.Where(v => v.Layout == layout.Value);

        IReadOnlyList<VirtualVillage> result = query.OrderByDescending(v => v.CreatedAtUtc).ToList();
        return Task.FromResult(result);
    }

    public Task<VillageLot> AddLotAsync(VillageLot lot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lot);

        if (!_villages.TryGetValue(lot.VillageId, out var village))
            throw new InvalidOperationException($"Village {lot.VillageId} not found.");

        var existingLots = _lots.Values.Count(l => l.VillageId == lot.VillageId);
        if (existingLots >= village.MaxLots)
            throw new InvalidOperationException($"Village {lot.VillageId} has reached its maximum of {village.MaxLots} lots.");

        if (lot.LotNumber <= 0)
            throw new ArgumentException("Lot number must be positive.", nameof(lot));

        if (lot.WidthMetres <= 0 || lot.DepthMetres <= 0)
            throw new ArgumentException("Lot dimensions must be positive.", nameof(lot));

        // Check for duplicate lot number in this village
        if (_lots.Values.Any(l => l.VillageId == lot.VillageId && l.LotNumber == lot.LotNumber))
            throw new InvalidOperationException($"Lot number {lot.LotNumber} already exists in village {lot.VillageId}.");

        var persisted = lot with { Id = lot.Id == Guid.Empty ? Guid.NewGuid() : lot.Id, Status = VillageLotStatus.Vacant, SitePlacementId = null };

        if (!_lots.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Lot with ID {persisted.Id} already exists.");

        _logger.LogInformation("Added lot {LotNumber} to village {VillageId}", persisted.LotNumber, persisted.VillageId);
        return Task.FromResult(persisted);
    }

    public Task<IReadOnlyList<VillageLot>> GetLotsAsync(Guid villageId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<VillageLot> result = _lots.Values
            .Where(l => l.VillageId == villageId)
            .OrderBy(l => l.LotNumber)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<VillageLot> AssignPlacementAsync(Guid lotId, Guid sitePlacementId, CancellationToken cancellationToken = default)
    {
        if (!_lots.TryGetValue(lotId, out var lot))
            throw new InvalidOperationException($"Lot {lotId} not found.");

        if (lot.Status != VillageLotStatus.Vacant && lot.Status != VillageLotStatus.Reserved)
            throw new InvalidOperationException($"Cannot assign placement to lot in status {lot.Status}.");

        var updated = lot with { SitePlacementId = sitePlacementId, Status = VillageLotStatus.Occupied };
        _lots[lotId] = updated;

        _logger.LogInformation("Assigned placement {PlacementId} to lot {LotId}", sitePlacementId, lotId);
        return Task.FromResult(updated);
    }

    public Task<(int TotalLots, int OccupiedLots, int VacantLots)> GetStatsAsync(Guid villageId, CancellationToken cancellationToken = default)
    {
        if (!_villages.ContainsKey(villageId))
            throw new InvalidOperationException($"Village {villageId} not found.");

        var lots = _lots.Values.Where(l => l.VillageId == villageId).ToList();
        var total = lots.Count;
        var occupied = lots.Count(l => l.Status == VillageLotStatus.Occupied);
        var vacant = lots.Count(l => l.Status == VillageLotStatus.Vacant);

        return Task.FromResult((total, occupied, vacant));
    }
}
