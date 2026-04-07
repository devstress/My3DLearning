namespace Terranes.Contracts.Enums;

/// <summary>
/// Status of a quote request through its lifecycle.
/// </summary>
public enum QuoteStatus
{
    /// <summary>Quote request has been submitted but not yet processed.</summary>
    Pending,

    /// <summary>Quotes are being collected from partners.</summary>
    InProgress,

    /// <summary>All partner quotes have been received and aggregated.</summary>
    Completed,

    /// <summary>One or more partners could not provide a quote.</summary>
    PartiallyCompleted,

    /// <summary>Quote request has expired without completion.</summary>
    Expired,

    /// <summary>Quote request was cancelled by the user.</summary>
    Cancelled
}
