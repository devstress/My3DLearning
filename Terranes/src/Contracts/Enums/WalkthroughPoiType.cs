namespace Terranes.Contracts.Enums;

/// <summary>
/// Types of points of interest in a 3D home walkthrough.
/// </summary>
public enum WalkthroughPoiType
{
    /// <summary>A room label / room entry point.</summary>
    Room,

    /// <summary>A design feature or highlight.</summary>
    Feature,

    /// <summary>A measurement or dimension annotation.</summary>
    Measurement,

    /// <summary>A furniture/fixture annotation.</summary>
    Fixture,

    /// <summary>An outdoor area or garden feature.</summary>
    Outdoor
}
