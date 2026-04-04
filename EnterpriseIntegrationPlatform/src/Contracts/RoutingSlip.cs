namespace EnterpriseIntegrationPlatform.Contracts;

/// <summary>
/// An ordered list of <see cref="RoutingSlipStep"/> entries attached to a message.
/// As each step completes, it is consumed from the slip and the message advances
/// to the next step. When no steps remain the slip is complete.
/// </summary>
/// <remarks>
/// <para>
/// This is the Enterprise Integration Patterns "Routing Slip" pattern — it enables
/// dynamic, per-message processing pipelines where each message carries its own
/// processing itinerary. This replaces static pipeline configuration and is
/// analogous to BizTalk dynamic send ports.
/// </para>
/// <para>
/// The slip is stored in the envelope's <c>Metadata</c> dictionary under the key
/// <see cref="MetadataKey"/> as a serialised JSON array of <see cref="RoutingSlipStep"/>.
/// </para>
/// </remarks>
/// <param name="Steps">Ordered list of processing steps still to be executed.</param>
public sealed record RoutingSlip(IReadOnlyList<RoutingSlipStep> Steps)
{
    /// <summary>Metadata key under which the routing slip is stored on an envelope.</summary>
    public const string MetadataKey = "RoutingSlip";

    /// <summary>Returns <c>true</c> when all steps have been consumed.</summary>
    public bool IsComplete => Steps.Count == 0;

    /// <summary>
    /// Returns the current (first) step, or <see langword="null"/> if the slip is complete.
    /// </summary>
    public RoutingSlipStep? CurrentStep => Steps.Count > 0 ? Steps[0] : null;

    /// <summary>
    /// Returns a new <see cref="RoutingSlip"/> with the current step consumed
    /// (removed from the front of the list).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the slip is already complete.</exception>
    public RoutingSlip Advance()
    {
        if (IsComplete)
            throw new InvalidOperationException("Cannot advance a completed routing slip.");

        return new RoutingSlip(Steps.Skip(1).ToList().AsReadOnly());
    }
}
