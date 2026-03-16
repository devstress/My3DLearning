using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Translator;

/// <summary>
/// The result of a <see cref="IMessageTranslator{TIn,TOut}"/> translation operation.
/// </summary>
/// <typeparam name="TOut">Target payload type of the translated envelope.</typeparam>
/// <param name="TranslatedEnvelope">The fully-populated translated envelope that was published.</param>
/// <param name="SourceMessageId">The <see cref="IntegrationEnvelope{T}.MessageId"/> of the source envelope.</param>
/// <param name="TargetTopic">The topic to which the translated envelope was published.</param>
public sealed record TranslationResult<TOut>(
    IntegrationEnvelope<TOut> TranslatedEnvelope,
    Guid SourceMessageId,
    string TargetTopic);
