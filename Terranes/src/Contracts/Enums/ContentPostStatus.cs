namespace Terranes.Contracts.Enums;

/// <summary>
/// Status of a user-generated content post.
/// </summary>
public enum ContentPostStatus
{
    /// <summary>Draft — not yet published.</summary>
    Draft,

    /// <summary>Published and visible to the community.</summary>
    Published,

    /// <summary>Under review by moderators.</summary>
    UnderReview,

    /// <summary>Removed by moderation.</summary>
    Removed,

    /// <summary>Archived by the owner.</summary>
    Archived
}
