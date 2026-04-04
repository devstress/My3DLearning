using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace EnterpriseIntegrationPlatform.Contracts;

/// <summary>
/// JSON-based implementation of <see cref="IMessagingMapper{TDomain}"/>.
/// Maps domain objects to and from <see cref="IntegrationEnvelope{TDomain}"/> instances,
/// preserving all metadata and correlation context.
/// Thread-safe and designed for use as a singleton or scoped service.
/// </summary>
/// <typeparam name="TDomain">The domain object type.</typeparam>
public sealed class JsonMessagingMapper<TDomain> : IMessagingMapper<TDomain>
{
    private readonly ILogger<JsonMessagingMapper<TDomain>> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of <see cref="JsonMessagingMapper{TDomain}"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="serializerOptions">
    /// Optional JSON serializer options. When <c>null</c>, default web-friendly options are used.
    /// </param>
    public JsonMessagingMapper(
        ILogger<JsonMessagingMapper<TDomain>> logger,
        JsonSerializerOptions? serializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    /// <inheritdoc />
    public IntegrationEnvelope<TDomain> ToEnvelope(
        TDomain domain,
        string source,
        string messageType,
        Dictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);

        var envelope = IntegrationEnvelope<TDomain>.Create(domain, source, messageType);

        var envelopeMetadata = new Dictionary<string, string>(envelope.Metadata);
        envelopeMetadata[MessageHeaders.ContentType] = "application/json";

        // Capture the CLR type for deserialization hints
        envelopeMetadata["clr-type"] = typeof(TDomain).FullName ?? typeof(TDomain).Name;

        if (metadata is not null)
        {
            foreach (var (key, value) in metadata)
            {
                envelopeMetadata[key] = value;
            }
        }

        var result = envelope with { Metadata = envelopeMetadata };

        _logger.LogDebug(
            "Mapped domain object {DomainType} to envelope {MessageId} with type {MessageType}",
            typeof(TDomain).Name,
            result.MessageId,
            messageType);

        return result;
    }

    /// <inheritdoc />
    public TDomain FromEnvelope(IntegrationEnvelope<TDomain> envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.Payload is null)
        {
            throw new InvalidOperationException(
                $"Envelope {envelope.MessageId} has a null payload. " +
                "Cannot extract domain object from an empty envelope.");
        }

        _logger.LogDebug(
            "Extracted domain object {DomainType} from envelope {MessageId}",
            typeof(TDomain).Name,
            envelope.MessageId);

        return envelope.Payload;
    }

    /// <inheritdoc />
    public IntegrationEnvelope<TDomain> ToChildEnvelope<TParent>(
        TDomain domain,
        IntegrationEnvelope<TParent> parent,
        string source,
        string messageType,
        Dictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);

        var childMetadata = new Dictionary<string, string>(parent.Metadata);
        childMetadata[MessageHeaders.ContentType] = "application/json";
        childMetadata["clr-type"] = typeof(TDomain).FullName ?? typeof(TDomain).Name;

        if (metadata is not null)
        {
            foreach (var (key, value) in metadata)
            {
                childMetadata[key] = value;
            }
        }

        var result = new IntegrationEnvelope<TDomain>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = parent.CorrelationId,
            CausationId = parent.MessageId,
            Timestamp = DateTimeOffset.UtcNow,
            Source = source,
            MessageType = messageType,
            Payload = domain,
            Metadata = childMetadata,
            SchemaVersion = parent.SchemaVersion,
        };

        _logger.LogDebug(
            "Mapped domain object {DomainType} to child envelope {MessageId} " +
            "(parent={ParentId}, correlation={CorrelationId})",
            typeof(TDomain).Name,
            result.MessageId,
            parent.MessageId,
            result.CorrelationId);

        return result;
    }
}
