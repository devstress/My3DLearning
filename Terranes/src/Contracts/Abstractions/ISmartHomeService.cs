using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for managing smart home suppliers — device catalog, compatibility checks, and pricing.
/// </summary>
public interface ISmartHomeService
{
    Task<SmartHomeDevice> AddDeviceAsync(SmartHomeDevice device, CancellationToken cancellationToken = default);
    Task<SmartHomeDevice?> GetDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SmartHomeDevice>> SearchDevicesAsync(SmartHomeCategory? category = null, string? protocol = null, decimal? maxPrice = null, CancellationToken cancellationToken = default);
    Task<bool> CheckCompatibilityAsync(Guid deviceId1, Guid deviceId2, CancellationToken cancellationToken = default);
    Task<decimal> CalculatePackagePriceAsync(IReadOnlyList<Guid> deviceIds, CancellationToken cancellationToken = default);
}
