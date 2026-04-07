namespace Terranes.Contracts.Enums;

/// <summary>
/// Represents the current stage of a buyer's journey through the Terranes platform.
/// </summary>
public enum JourneyStage
{
    /// <summary>Buyer is browsing the virtual village.</summary>
    Browsing,

    /// <summary>Buyer has selected a home design.</summary>
    DesignSelected,

    /// <summary>Buyer has placed the design onto their land block.</summary>
    PlacedOnLand,

    /// <summary>Buyer is customising the design (materials, layout, fixtures).</summary>
    Customising,

    /// <summary>Buyer has requested an end-to-end quote.</summary>
    QuoteRequested,

    /// <summary>Quote has been received with full cost breakdown.</summary>
    QuoteReceived,

    /// <summary>Buyer has been referred to partners.</summary>
    Referred,

    /// <summary>Journey is complete — buyer has engaged with partners.</summary>
    Completed,

    /// <summary>Buyer abandoned the journey.</summary>
    Abandoned
}
