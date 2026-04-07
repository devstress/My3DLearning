namespace Terranes.Contracts.Enums;

/// <summary>
/// Status of a walkthrough session.
/// </summary>
public enum WalkthroughStatus
{
    /// <summary>The walkthrough is being generated.</summary>
    Generating,

    /// <summary>The walkthrough is ready for viewing.</summary>
    Ready,

    /// <summary>Generation failed.</summary>
    Failed
}
