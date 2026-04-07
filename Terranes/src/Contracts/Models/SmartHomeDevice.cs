using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a smart home device/system for a home.
/// </summary>
public sealed record SmartHomeDevice(
    Guid Id,
    Guid SupplierId,
    string Name,
    SmartHomeCategory Category,
    decimal PriceAud,
    string CompatibilityProtocol,
    bool RequiresProfessionalInstall,
    string Sku);
