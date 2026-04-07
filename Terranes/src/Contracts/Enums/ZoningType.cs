namespace Terranes.Contracts.Enums;

/// <summary>
/// Zoning classifications for land blocks.
/// </summary>
public enum ZoningType
{
    /// <summary>Residential — standard housing.</summary>
    Residential,

    /// <summary>Residential — medium density (townhouses, units).</summary>
    ResidentialMediumDensity,

    /// <summary>Residential — high density (apartments).</summary>
    ResidentialHighDensity,

    /// <summary>Rural residential.</summary>
    RuralResidential,

    /// <summary>Commercial zoning.</summary>
    Commercial,

    /// <summary>Industrial zoning.</summary>
    Industrial,

    /// <summary>Mixed use — residential and commercial.</summary>
    MixedUse
}
