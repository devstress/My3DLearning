using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.SitePlacementEngine;

/// <summary>
/// In-memory implementation of <see cref="ISitePlacementService"/>.
/// Places 3D home models onto land blocks and validates site fit using real dimensions.
/// </summary>
public sealed class SitePlacementService : ISitePlacementService
{
    private readonly ConcurrentDictionary<Guid, Contracts.Models.SitePlacement> _store = new();
    private readonly IHomeModelService _homeModelService;
    private readonly ILandBlockService _landBlockService;
    private readonly ILogger<SitePlacementService> _logger;

    public SitePlacementService(
        IHomeModelService homeModelService,
        ILandBlockService landBlockService,
        ILogger<SitePlacementService> logger)
    {
        _homeModelService = homeModelService;
        _landBlockService = landBlockService;
        _logger = logger;
    }

    public Task<Contracts.Models.SitePlacement> PlaceAsync(Contracts.Models.SitePlacement placement, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(placement);

        if (placement.ScaleFactor <= 0)
            throw new ArgumentException("Scale factor must be positive.", nameof(placement));

        if (placement.RotationDegrees < 0 || placement.RotationDegrees >= 360)
            throw new ArgumentException("Rotation must be between 0 and 359.999 degrees.", nameof(placement));

        var persisted = placement with
        {
            Id = placement.Id == Guid.Empty ? Guid.NewGuid() : placement.Id,
            PlacedAtUtc = DateTimeOffset.UtcNow
        };

        if (!_store.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Site placement with ID {persisted.Id} already exists.");

        _logger.LogInformation("Placed model {ModelId} on block {BlockId} at ({X}, {Y})",
            persisted.HomeModelId, persisted.LandBlockId, persisted.OffsetXMetres, persisted.OffsetYMetres);
        return Task.FromResult(persisted);
    }

    public Task<Contracts.Models.SitePlacement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var placement);
        return Task.FromResult(placement);
    }

    /// <summary>
    /// Validates whether a home model fits on the specified land block.
    /// Uses the model's floor area and the block's frontage/depth to determine fit.
    /// Accounts for offset position, scale factor, and a minimum setback of 1.5 metres on each side.
    /// </summary>
    public async Task<bool> ValidateFitAsync(Contracts.Models.SitePlacement placement, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(placement);

        var model = await _homeModelService.GetByIdAsync(placement.HomeModelId, cancellationToken);
        if (model is null)
        {
            _logger.LogWarning("ValidateFit failed: model {ModelId} not found", placement.HomeModelId);
            return false;
        }

        var block = await _landBlockService.GetByIdAsync(placement.LandBlockId, cancellationToken);
        if (block is null)
        {
            _logger.LogWarning("ValidateFit failed: block {BlockId} not found", placement.LandBlockId);
            return false;
        }

        // Approximate the model footprint as a square based on floor area, then scale
        var footprintSide = Math.Sqrt(model.FloorAreaSquareMetres) * placement.ScaleFactor;
        const double minimumSetbackMetres = 1.5;

        var usableFrontage = block.FrontageMetres - 2 * minimumSetbackMetres;
        var usableDepth = block.DepthMetres - 2 * minimumSetbackMetres;

        // Check the model footprint + offset fit within usable area
        var fitsWidth = placement.OffsetXMetres + footprintSide <= usableFrontage && placement.OffsetXMetres >= 0;
        var fitsDepth = placement.OffsetYMetres + footprintSide <= usableDepth && placement.OffsetYMetres >= 0;

        var fits = fitsWidth && fitsDepth;

        _logger.LogInformation(
            "ValidateFit for model {ModelId} on block {BlockId}: {Result} (footprint={Footprint:F1}m, usable={Width:F1}x{Depth:F1}m)",
            model.Id, block.Id, fits ? "FITS" : "DOES_NOT_FIT", footprintSide, usableFrontage, usableDepth);

        return fits;
    }
}
