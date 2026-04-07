namespace Terranes.Contracts.Enums;

/// <summary>
/// Status of a lot within a virtual village.
/// </summary>
public enum VillageLotStatus
{
    /// <summary>Lot is available and has no home placed on it.</summary>
    Vacant,

    /// <summary>A home model has been placed on this lot.</summary>
    Occupied,

    /// <summary>The lot is reserved but not yet built on.</summary>
    Reserved,

    /// <summary>Lot is under construction / being designed.</summary>
    UnderDesign
}
