using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Interface for message transformation operations.
/// Translates a message payload from one format to another.
/// Equivalent to BizTalk Maps (XSLT transforms, Functoids).
/// </summary>
public interface IMessageTransformer<TIn, TOut>
{
    /// <summary>
    /// Transforms the payload of a message from <typeparamref name="TIn"/>
    /// to <typeparamref name="TOut"/>, preserving envelope metadata.
    /// </summary>
    IntegrationEnvelope<TOut> Transform(IntegrationEnvelope<TIn> input);
}
