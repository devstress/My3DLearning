namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// Control channel through which downstream participants register and unregister
/// their routing preferences with the <see cref="IDynamicRouter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Participants publish control messages to advertise which message types or
/// conditions they can handle. The Dynamic Router maintains an internal routing
/// table that is updated on every registration or un-registration.
/// </para>
/// <para>
/// Thread-safety: implementations must be safe for concurrent use by multiple
/// registration callers and the router evaluation path.
/// </para>
/// </remarks>
public interface IRouterControlChannel
{
    /// <summary>
    /// Registers a destination for a specific condition key.
    /// If a destination with the same <paramref name="conditionKey"/> already exists it is replaced.
    /// </summary>
    /// <param name="conditionKey">
    /// The value that identifies which messages should be routed to this destination.
    /// Matched against <see cref="DynamicRouterOptions.ConditionField"/> on the envelope.
    /// </param>
    /// <param name="destination">The target topic or subject.</param>
    /// <param name="participantId">
    /// Optional identifier of the registering participant. Used for diagnostics and
    /// targeted un-registration via <see cref="UnregisterAsync(string, CancellationToken)"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RegisterAsync(
        string conditionKey,
        string destination,
        string? participantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a previously registered destination by its <paramref name="conditionKey"/>.
    /// Returns <see langword="true"/> if the entry was found and removed;
    /// <see langword="false"/> if no entry with that key existed.
    /// </summary>
    /// <param name="conditionKey">The condition key whose registration should be removed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> UnregisterAsync(
        string conditionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a snapshot of the current routing table as a read-only dictionary.
    /// Keys are condition keys; values are <see cref="DynamicRouteEntry"/> descriptors.
    /// </summary>
    IReadOnlyDictionary<string, DynamicRouteEntry> GetRoutingTable();
}
