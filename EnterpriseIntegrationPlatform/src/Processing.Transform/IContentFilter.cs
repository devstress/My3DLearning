namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Enterprise Integration Pattern — Content Filter.
/// Removes unwanted fields from a message payload, keeping only the specified paths.
/// This is the inverse of content enrichment — it strips a message down to only the
/// data that the next processing step requires.
/// </summary>
public interface IContentFilter
{
    /// <summary>
    /// Filters the <paramref name="payload"/> to retain only the fields specified by
    /// <paramref name="keepPaths"/>. All other fields are removed.
    /// </summary>
    /// <param name="payload">The original JSON payload to filter.</param>
    /// <param name="keepPaths">
    /// Dot-separated property paths to retain (e.g. <c>order.id</c>,
    /// <c>customer.address.city</c>). Paths that do not exist in the payload are
    /// silently ignored.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A JSON payload containing only the specified fields.</returns>
    Task<string> FilterAsync(
        string payload,
        IReadOnlyList<string> keepPaths,
        CancellationToken cancellationToken = default);
}
