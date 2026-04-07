using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Immersive3D;

/// <summary>
/// In-memory implementation of <see cref="IWalkthroughService"/>.
/// Generates immersive 3D walkthroughs with room navigation and point-of-interest markers.
/// </summary>
public sealed class WalkthroughService : IWalkthroughService
{
    private readonly ConcurrentDictionary<Guid, HomeWalkthrough> _walkthroughs = new();
    private readonly ConcurrentDictionary<Guid, WalkthroughPoi> _pois = new();
    private readonly ILogger<WalkthroughService> _logger;

    public WalkthroughService(ILogger<WalkthroughService> logger) => _logger = logger;

    public Task<HomeWalkthrough> GenerateAsync(Guid homeModelId, Guid? sitePlacementId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (homeModelId == Guid.Empty)
            throw new ArgumentException("Home model ID is required.", nameof(homeModelId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        // Simulate walkthrough generation — in production this would analyse the 3D model
        var walkthrough = new HomeWalkthrough(
            Guid.NewGuid(), homeModelId, sitePlacementId,
            WalkthroughStatus.Ready, // Immediate for in-memory impl
            TotalRooms: 6, // Simulated room count
            DurationSeconds: 180, // 3-minute default tour
            userId, DateTimeOffset.UtcNow);

        if (!_walkthroughs.TryAdd(walkthrough.Id, walkthrough))
            throw new InvalidOperationException($"Walkthrough {walkthrough.Id} already exists.");

        _logger.LogInformation("Generated walkthrough {WalkthroughId} for model {ModelId}", walkthrough.Id, homeModelId);
        return Task.FromResult(walkthrough);
    }

    public Task<HomeWalkthrough?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _walkthroughs.TryGetValue(id, out var walkthrough);
        return Task.FromResult(walkthrough);
    }

    public Task<IReadOnlyList<HomeWalkthrough>> GetByHomeModelAsync(Guid homeModelId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HomeWalkthrough> result = _walkthroughs.Values
            .Where(w => w.HomeModelId == homeModelId)
            .OrderByDescending(w => w.CreatedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<WalkthroughPoi> AddPoiAsync(WalkthroughPoi poi, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(poi);

        if (!_walkthroughs.TryGetValue(poi.WalkthroughId, out var walkthrough))
            throw new InvalidOperationException($"Walkthrough {poi.WalkthroughId} not found.");

        if (walkthrough.Status != WalkthroughStatus.Ready)
            throw new InvalidOperationException($"Cannot add POIs to walkthrough in status {walkthrough.Status}.");

        if (string.IsNullOrWhiteSpace(poi.Label))
            throw new ArgumentException("POI label is required.", nameof(poi));

        var persisted = poi with { Id = poi.Id == Guid.Empty ? Guid.NewGuid() : poi.Id };

        if (!_pois.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"POI {persisted.Id} already exists.");

        _logger.LogInformation("Added POI {PoiId} ({Type}) to walkthrough {WalkthroughId}", persisted.Id, persisted.Type, persisted.WalkthroughId);
        return Task.FromResult(persisted);
    }

    public Task<IReadOnlyList<WalkthroughPoi>> GetPoisAsync(Guid walkthroughId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WalkthroughPoi> result = _pois.Values
            .Where(p => p.WalkthroughId == walkthroughId)
            .OrderBy(p => p.Label)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<WalkthroughPoi>> GetPoisByRoomAsync(Guid walkthroughId, string roomName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomName))
            throw new ArgumentException("Room name is required.", nameof(roomName));

        IReadOnlyList<WalkthroughPoi> result = _pois.Values
            .Where(p => p.WalkthroughId == walkthroughId &&
                        string.Equals(p.RoomName, roomName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Label)
            .ToList();
        return Task.FromResult(result);
    }
}
