using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for managing furniture and interior suppliers — catalog, room fitting, and pricing.
/// </summary>
public interface IFurnitureService
{
    Task<FurnitureItem> AddItemAsync(FurnitureItem item, CancellationToken cancellationToken = default);
    Task<FurnitureItem?> GetItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FurnitureItem>> SearchCatalogAsync(FurnitureCategory? category = null, decimal? maxPrice = null, Guid? supplierId = null, CancellationToken cancellationToken = default);
    Task<RoomFitting> FitItemAsync(RoomFitting fitting, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomFitting>> GetFittingsForModelAsync(Guid homeModelId, CancellationToken cancellationToken = default);
    Task<decimal> CalculateTotalAsync(Guid homeModelId, CancellationToken cancellationToken = default);
}
