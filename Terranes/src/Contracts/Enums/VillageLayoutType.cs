namespace Terranes.Contracts.Enums;

/// <summary>
/// Layout type for a virtual village arrangement.
/// </summary>
public enum VillageLayoutType
{
    /// <summary>Grid-based layout with regular lot spacing.</summary>
    Grid,

    /// <summary>Curved street layout following terrain contours.</summary>
    Curved,

    /// <summary>Cul-de-sac arrangement with a shared turning circle.</summary>
    CulDeSac,

    /// <summary>Mixed-use layout combining residential and commercial lots.</summary>
    MixedUse
}
