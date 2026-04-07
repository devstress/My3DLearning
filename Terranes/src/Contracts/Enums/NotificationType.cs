namespace Terranes.Contracts.Enums;

/// <summary>
/// Represents the type of notification sent to a user or partner.
/// </summary>
public enum NotificationType
{
    /// <summary>Quote is ready for review.</summary>
    QuoteReady,

    /// <summary>Partner referral has been created.</summary>
    ReferralCreated,

    /// <summary>Partner accepted a referral.</summary>
    ReferralAccepted,

    /// <summary>Design edit saved successfully.</summary>
    DesignSaved,

    /// <summary>Video-to-3D processing completed.</summary>
    VideoProcessingComplete,

    /// <summary>New content post published.</summary>
    ContentPublished,

    /// <summary>System health alert.</summary>
    SystemAlert,

    /// <summary>General information notification.</summary>
    Info
}
