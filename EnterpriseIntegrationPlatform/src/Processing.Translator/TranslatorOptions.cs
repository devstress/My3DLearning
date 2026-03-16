namespace EnterpriseIntegrationPlatform.Processing.Translator;

/// <summary>
/// Configuration options for <see cref="MessageTranslator{TIn,TOut}"/>.
/// Bind from the <c>MessageTranslator</c> configuration section.
/// </summary>
public sealed class TranslatorOptions
{
    /// <summary>
    /// Topic to which the translated envelope is published.
    /// Must be set; an empty value causes <see cref="MessageTranslator{TIn,TOut}"/> to
    /// throw <see cref="InvalidOperationException"/> at translation time.
    /// </summary>
    public string TargetTopic { get; set; } = string.Empty;

    /// <summary>
    /// Message type to assign to the translated envelope.
    /// When <see langword="null"/> or empty, the source envelope's
    /// <see cref="Contracts.IntegrationEnvelope{T}.MessageType"/> is preserved.
    /// </summary>
    public string? TargetMessageType { get; set; }

    /// <summary>
    /// Source identifier to assign to the translated envelope.
    /// When <see langword="null"/> or empty, the source envelope's
    /// <see cref="Contracts.IntegrationEnvelope{T}.Source"/> is preserved.
    /// </summary>
    public string? TargetSource { get; set; }

    /// <summary>
    /// Field mappings used by <see cref="JsonFieldMappingTransform"/> to map source JSON
    /// fields to target JSON fields.
    /// Ignored by other <see cref="IPayloadTransform{TIn,TOut}"/> implementations.
    /// </summary>
    public List<FieldMapping> FieldMappings { get; set; } = [];
}
