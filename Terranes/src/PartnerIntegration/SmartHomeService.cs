using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.PartnerIntegration;

/// <summary>
/// In-memory implementation of <see cref="ISmartHomeService"/>.
/// Manages smart home device catalog, compatibility checks, and pricing.
/// </summary>
public sealed class SmartHomeService : ISmartHomeService
{
    private readonly ConcurrentDictionary<Guid, SmartHomeDevice> _devices = new();
    private readonly ILogger<SmartHomeService> _logger;

    public SmartHomeService(ILogger<SmartHomeService> logger) => _logger = logger;

    public Task<SmartHomeDevice> AddDeviceAsync(SmartHomeDevice device, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (string.IsNullOrWhiteSpace(device.Name))
            throw new ArgumentException("Device name is required.", nameof(device));

        if (device.PriceAud < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(device));

        if (string.IsNullOrWhiteSpace(device.Sku))
            throw new ArgumentException("SKU is required.", nameof(device));

        if (string.IsNullOrWhiteSpace(device.CompatibilityProtocol))
            throw new ArgumentException("Compatibility protocol is required.", nameof(device));

        var persisted = device with { Id = device.Id == Guid.Empty ? Guid.NewGuid() : device.Id };

        if (!_devices.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Smart home device {persisted.Id} already exists.");

        _logger.LogInformation("Added smart home device {DeviceId} ({Category})", persisted.Id, persisted.Category);
        return Task.FromResult(persisted);
    }

    public Task<SmartHomeDevice?> GetDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        _devices.TryGetValue(deviceId, out var device);
        return Task.FromResult(device);
    }

    public Task<IReadOnlyList<SmartHomeDevice>> SearchDevicesAsync(SmartHomeCategory? category = null, string? protocol = null, decimal? maxPrice = null, CancellationToken cancellationToken = default)
    {
        var query = _devices.Values.AsEnumerable();

        if (category.HasValue)
            query = query.Where(d => d.Category == category.Value);

        if (!string.IsNullOrWhiteSpace(protocol))
            query = query.Where(d => d.CompatibilityProtocol.Equals(protocol, StringComparison.OrdinalIgnoreCase));

        if (maxPrice.HasValue)
            query = query.Where(d => d.PriceAud <= maxPrice.Value);

        IReadOnlyList<SmartHomeDevice> result = query.OrderBy(d => d.PriceAud).ToList();
        return Task.FromResult(result);
    }

    /// <summary>
    /// Checks if two devices are compatible based on their communication protocol.
    /// Devices sharing the same protocol are considered compatible.
    /// </summary>
    public Task<bool> CheckCompatibilityAsync(Guid deviceId1, Guid deviceId2, CancellationToken cancellationToken = default)
    {
        if (!_devices.TryGetValue(deviceId1, out var device1))
            throw new InvalidOperationException($"Device {deviceId1} not found.");

        if (!_devices.TryGetValue(deviceId2, out var device2))
            throw new InvalidOperationException($"Device {deviceId2} not found.");

        var compatible = device1.CompatibilityProtocol.Equals(device2.CompatibilityProtocol, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(compatible);
    }

    /// <summary>
    /// Calculates the total price for a package of smart home devices.
    /// </summary>
    public Task<decimal> CalculatePackagePriceAsync(IReadOnlyList<Guid> deviceIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(deviceIds);

        var total = 0m;
        foreach (var id in deviceIds)
        {
            if (!_devices.TryGetValue(id, out var device))
                throw new InvalidOperationException($"Device {id} not found.");
            total += device.PriceAud;
        }
        return Task.FromResult(total);
    }
}
