namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a real estate agent profile in the platform.
/// </summary>
public sealed record AgentProfile(
    Guid PartnerId,
    string LicenseNumber,
    IReadOnlyList<string> CoverageSuburbs,
    decimal CommissionPercentage,
    int ActiveListingsCount,
    bool AcceptsSelfListings);
