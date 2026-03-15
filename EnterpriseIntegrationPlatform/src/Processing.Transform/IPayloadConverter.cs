namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Converts a payload from one format to another as part of the Message Translator EIP pattern.
/// </summary>
/// <typeparam name="TIn">The source payload type.</typeparam>
/// <typeparam name="TOut">The target payload type.</typeparam>
public interface IPayloadConverter<in TIn, TOut>
{
    /// <summary>
    /// Converts <paramref name="input"/> from the source format to the target format.
    /// </summary>
    /// <param name="input">The payload to convert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The converted payload.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="input"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="input"/> cannot be parsed in the expected source format.
    /// </exception>
    Task<TOut> ConvertAsync(TIn input, CancellationToken cancellationToken = default);
}
