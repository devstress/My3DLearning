namespace EnterpriseIntegrationPlatform.Processing.Translator;

/// <summary>
/// Transforms a payload of type <typeparamref name="TIn"/> into a payload of type
/// <typeparamref name="TOut"/>. Implementations carry the domain-specific transformation
/// logic and are injected into <see cref="MessageTranslator{TIn,TOut}"/>.
/// </summary>
/// <typeparam name="TIn">Source payload type.</typeparam>
/// <typeparam name="TOut">Target payload type.</typeparam>
public interface IPayloadTransform<TIn, TOut>
{
    /// <summary>Transforms the <paramref name="source"/> payload into the target type.</summary>
    /// <param name="source">The source payload to transform.</param>
    /// <returns>The transformed payload.</returns>
    TOut Transform(TIn source);
}
