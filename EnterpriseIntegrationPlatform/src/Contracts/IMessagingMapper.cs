using System.Text.Json;

namespace EnterpriseIntegrationPlatform.Contracts;

/// <summary>
/// Maps domain objects to and from <see cref="IntegrationEnvelope{T}"/> instances.
/// Implements the Messaging Mapper Enterprise Integration Pattern — isolates
/// the domain model from messaging infrastructure so that business objects
/// never depend on the envelope format.
/// </summary>
/// <typeparam name="TDomain">The domain object type.</typeparam>
public interface IMessagingMapper<TDomain>
{
    /// <summary>
    /// Maps a domain object into an <see cref="IntegrationEnvelope{TDomain}"/>,
    /// attaching all required envelope metadata.
    /// </summary>
    /// <param name="domain">The domain object to wrap.</param>
    /// <param name="source">The name of the originating service or system.</param>
    /// <param name="messageType">The logical message type name.</param>
    /// <param name="metadata">Optional metadata key-value pairs to attach to the envelope.</param>
    /// <returns>A fully populated <see cref="IntegrationEnvelope{TDomain}"/>.</returns>
    IntegrationEnvelope<TDomain> ToEnvelope(
        TDomain domain,
        string source,
        string messageType,
        Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Extracts the domain object from an <see cref="IntegrationEnvelope{TDomain}"/>.
    /// </summary>
    /// <param name="envelope">The envelope containing the domain object.</param>
    /// <returns>The extracted domain object.</returns>
    TDomain FromEnvelope(IntegrationEnvelope<TDomain> envelope);

    /// <summary>
    /// Maps a domain object into an <see cref="IntegrationEnvelope{TDomain}"/>
    /// that is a child of an existing envelope (preserving correlation ID and causation chain).
    /// </summary>
    /// <param name="domain">The domain object to wrap.</param>
    /// <param name="parent">The parent envelope whose correlation context is preserved.</param>
    /// <param name="source">The name of the originating service or system.</param>
    /// <param name="messageType">The logical message type name.</param>
    /// <param name="metadata">Optional metadata key-value pairs to attach to the envelope.</param>
    /// <returns>A fully populated <see cref="IntegrationEnvelope{TDomain}"/> with parent context.</returns>
    IntegrationEnvelope<TDomain> ToChildEnvelope<TParent>(
        TDomain domain,
        IntegrationEnvelope<TParent> parent,
        string source,
        string messageType,
        Dictionary<string, string>? metadata = null);
}
