namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Carries a payload through a transformation pipeline together with its content type
/// and accumulated metadata. Each <see cref="ITransformStep"/> receives the current
/// context and returns an updated context with the transformed payload.
/// </summary>
public sealed class TransformContext
{
    /// <summary>
    /// Initialises a new <see cref="TransformContext"/> with the supplied payload and
    /// content type.
    /// </summary>
    /// <param name="payload">The raw payload string (JSON, XML, plain text, etc.).</param>
    /// <param name="contentType">
    /// MIME-style content type indicator (e.g. <c>application/json</c>,
    /// <c>application/xml</c>, <c>text/plain</c>).
    /// </param>
    public TransformContext(string payload, string contentType)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        Payload = payload;
        ContentType = contentType;
    }

    /// <summary>The raw payload string.</summary>
    public string Payload { get; }

    /// <summary>
    /// MIME-style content type indicator that describes the current format of
    /// <see cref="Payload"/> (e.g. <c>application/json</c>, <c>application/xml</c>).
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Mutable metadata bag that transform steps can write to in order to communicate
    /// information downstream (e.g. which steps were applied, intermediate counts, etc.).
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Creates a new <see cref="TransformContext"/> with a different payload and content
    /// type, preserving existing <see cref="Metadata"/>.
    /// </summary>
    public TransformContext WithPayload(string payload, string contentType) =>
        new(payload, contentType) { Metadata = Metadata };

    /// <summary>
    /// Creates a new <see cref="TransformContext"/> with a different payload, preserving
    /// the current <see cref="ContentType"/> and <see cref="Metadata"/>.
    /// </summary>
    public TransformContext WithPayload(string payload) =>
        new(payload, ContentType) { Metadata = Metadata };
}
