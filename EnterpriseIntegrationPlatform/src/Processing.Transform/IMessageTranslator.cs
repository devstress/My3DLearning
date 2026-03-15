using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Transforms the payload of an <see cref="IntegrationEnvelope{T}"/> from one format to
/// another, implementing the Message Translator Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// The translated envelope preserves the original <see cref="IntegrationEnvelope{T}.CorrelationId"/>,
/// <see cref="IntegrationEnvelope{T}.Source"/>, and <see cref="IntegrationEnvelope{T}.Metadata"/>.
/// A new <see cref="IntegrationEnvelope{T}.MessageId"/> is generated for the output envelope, and
/// <see cref="IntegrationEnvelope{T}.CausationId"/> is set to the source envelope's
/// <see cref="IntegrationEnvelope{T}.MessageId"/> to preserve the message lineage.
/// </remarks>
public interface IMessageTranslator
{
    /// <summary>
    /// Translates the payload of <paramref name="envelope"/> using the supplied
    /// <paramref name="converter"/> and returns a new envelope carrying the converted payload.
    /// </summary>
    /// <typeparam name="TIn">The source payload type.</typeparam>
    /// <typeparam name="TOut">The target payload type.</typeparam>
    /// <param name="envelope">The source message envelope to translate.</param>
    /// <param name="converter">The payload converter that transforms the payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A new <see cref="IntegrationEnvelope{TOut}"/> carrying the converted payload and
    /// the traceability headers of the source envelope.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="envelope"/> or <paramref name="converter"/> is
    /// <see langword="null"/>.
    /// </exception>
    Task<IntegrationEnvelope<TOut>> TranslateAsync<TIn, TOut>(
        IntegrationEnvelope<TIn> envelope,
        IPayloadConverter<TIn, TOut> converter,
        CancellationToken cancellationToken = default);
}
