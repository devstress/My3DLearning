namespace Terranes.Contracts.Enums;

/// <summary>
/// Types of 3D edits that can be applied in the real-time editor.
/// </summary>
public enum EditOperationType
{
    /// <summary>Move the model to a new position.</summary>
    Move,

    /// <summary>Rotate the model.</summary>
    Rotate,

    /// <summary>Scale the model.</summary>
    Scale,

    /// <summary>Change a material on a surface.</summary>
    MaterialChange,

    /// <summary>Swap a fixture or component.</summary>
    ComponentSwap,

    /// <summary>Change the colour of a surface.</summary>
    ColourChange
}
