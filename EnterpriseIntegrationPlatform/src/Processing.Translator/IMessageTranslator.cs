using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Translator;

/// <summary>
/// Implements the Message Translator Enterprise Integration Pattern.
/// Translates an <see cref="IntegrationEnvelope{TIn}"/> into an
/// <see cref="IntegrationEnvelope{TOut}"/> and publishes the result to the configured
/// target topic.
/// </summary>
/// <typeparam name="TIn">Source payload type.</typeparam>
/// <typeparam name="TOut">Target payload type.</typeparam>
public interface IMessageTranslator<TIn, TOut>
{
    /// <summary>
    /// Translates the <paramref name="source"/> envelope, publishes the translated envelope
    /// to the configured target topic, and returns the translation result.
    /// </summary>
    /// <param name="source">The envelope to translate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="TranslationResult{TOut}"/> containing the translated envelope,
    /// the source message identifier, and the target topic.
    /// </returns>
    Task<TranslationResult<TOut>> TranslateAsync(
        IntegrationEnvelope<TIn> source,
        CancellationToken cancellationToken = default);
}
