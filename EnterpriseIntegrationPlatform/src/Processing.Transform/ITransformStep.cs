namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// A single step in a transformation pipeline. Implementations perform one atomic
/// transformation (e.g. JSON→XML conversion, regex replacement, JSONPath filtering)
/// and return an updated <see cref="TransformContext"/>.
/// </summary>
public interface ITransformStep
{
    /// <summary>
    /// Human-readable name used for logging and diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the transformation on the supplied <paramref name="context"/> and returns
    /// a new <see cref="TransformContext"/> containing the transformed payload.
    /// </summary>
    /// <param name="context">Current pipeline context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An updated <see cref="TransformContext"/>.</returns>
    Task<TransformContext> ExecuteAsync(
        TransformContext context,
        CancellationToken cancellationToken = default);
}
