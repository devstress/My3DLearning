namespace Terranes.Contracts.Enums;

/// <summary>
/// Type of analytics event tracked by the platform.
/// </summary>
public enum AnalyticsEventType
{
    /// <summary>Buyer viewed a village.</summary>
    VillageView,

    /// <summary>Buyer viewed a home model.</summary>
    HomeModelView,

    /// <summary>Buyer started a walkthrough.</summary>
    WalkthroughStarted,

    /// <summary>Buyer placed a design on land.</summary>
    DesignPlaced,

    /// <summary>Buyer made a design edit.</summary>
    DesignEdited,

    /// <summary>Buyer requested a quote.</summary>
    QuoteRequested,

    /// <summary>Buyer was referred to a partner.</summary>
    Referral,

    /// <summary>Buyer searched for content.</summary>
    Search,

    /// <summary>User registered on the platform.</summary>
    Registration,

    /// <summary>User logged in.</summary>
    Login
}
