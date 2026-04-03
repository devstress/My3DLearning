namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Executes an ordered sequence of <see cref="ITransformStep"/> instances against a
/// payload, returning the final <see cref="TransformResult"/>.
/// </summary>
public interface ITransformPipeline
{
    /// <summary>
    /// Runs all registered transform steps in order against the supplied
    /// <paramref name="payload"/> and returns the final result.
    /// </summary>
    /// <param name="payload">The raw input payload.</param>
    /// <param name="contentType">
    /// The MIME-style content type of <paramref name="payload"/>
    /// (e.g. <c>application/json</c>).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="TransformResult"/> containing the transformed payload.</returns>
    Task<TransformResult> ExecuteAsync(
        string payload,
        string contentType,
        CancellationToken cancellationToken = default);
}
