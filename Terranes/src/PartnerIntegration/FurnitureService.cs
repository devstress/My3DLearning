using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.PartnerIntegration;

/// <summary>
/// In-memory implementation of <see cref="IFurnitureService"/>.
/// Manages furniture supplier catalog, room fitting, and pricing.
/// </summary>
public sealed class FurnitureService : IFurnitureService
{
    private readonly ConcurrentDictionary<Guid, FurnitureItem> _catalog = new();
    private readonly ConcurrentDictionary<Guid, RoomFitting> _fittings = new();
    private readonly ILogger<FurnitureService> _logger;

    public FurnitureService(ILogger<FurnitureService> logger) => _logger = logger;

    public Task<FurnitureItem> AddItemAsync(FurnitureItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Item name is required.", nameof(item));

        if (item.PriceAud < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(item));

        if (string.IsNullOrWhiteSpace(item.Sku))
            throw new ArgumentException("SKU is required.", nameof(item));

        if (item.WidthMetres <= 0 || item.DepthMetres <= 0 || item.HeightMetres <= 0)
            throw new ArgumentException("All dimensions must be positive.", nameof(item));

        var persisted = item with { Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id };

        if (!_catalog.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Furniture item {persisted.Id} already exists.");

        _logger.LogInformation("Added furniture item {ItemId} ({Category})", persisted.Id, persisted.Category);
        return Task.FromResult(persisted);
    }

    public Task<FurnitureItem?> GetItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        _catalog.TryGetValue(itemId, out var item);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<FurnitureItem>> SearchCatalogAsync(FurnitureCategory? category = null, decimal? maxPrice = null, Guid? supplierId = null, CancellationToken cancellationToken = default)
    {
        var query = _catalog.Values.AsEnumerable();

        if (category.HasValue)
            query = query.Where(i => i.Category == category.Value);

        if (maxPrice.HasValue)
            query = query.Where(i => i.PriceAud <= maxPrice.Value);

        if (supplierId.HasValue)
            query = query.Where(i => i.SupplierId == supplierId.Value);

        IReadOnlyList<FurnitureItem> result = query.Where(i => i.InStock).OrderBy(i => i.PriceAud).ToList();
        return Task.FromResult(result);
    }

    public Task<RoomFitting> FitItemAsync(RoomFitting fitting, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fitting);

        if (string.IsNullOrWhiteSpace(fitting.RoomName))
            throw new ArgumentException("Room name is required.", nameof(fitting));

        if (!_catalog.ContainsKey(fitting.FurnitureItemId))
            throw new InvalidOperationException($"Furniture item {fitting.FurnitureItemId} not found in catalog.");

        if (fitting.RotationDegrees < 0 || fitting.RotationDegrees >= 360)
            throw new ArgumentException("Rotation must be between 0 and 359.999 degrees.", nameof(fitting));

        var persisted = fitting with { Id = fitting.Id == Guid.Empty ? Guid.NewGuid() : fitting.Id };

        if (!_fittings.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Room fitting {persisted.Id} already exists.");

        _logger.LogInformation("Fitted item {ItemId} in model {ModelId}", persisted.FurnitureItemId, persisted.HomeModelId);
        return Task.FromResult(persisted);
    }

    public Task<IReadOnlyList<RoomFitting>> GetFittingsForModelAsync(Guid homeModelId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RoomFitting> result = _fittings.Values
            .Where(f => f.HomeModelId == homeModelId)
            .OrderBy(f => f.RoomName)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<decimal> CalculateTotalAsync(Guid homeModelId, CancellationToken cancellationToken = default)
    {
        var fittings = _fittings.Values.Where(f => f.HomeModelId == homeModelId);
        var total = fittings.Sum(f => _catalog.TryGetValue(f.FurnitureItemId, out var item) ? item.PriceAud : 0m);
        return Task.FromResult(total);
    }
}
