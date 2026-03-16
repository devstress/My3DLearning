namespace EnterpriseIntegrationPlatform.Processing.Splitter;

/// <summary>
/// Defines a strategy for splitting a composite payload of type <typeparamref name="T"/>
/// into individual items. Implementations carry the domain-specific splitting logic and
/// are injected into <see cref="MessageSplitter{T}"/>.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public interface ISplitStrategy<T>
{
    /// <summary>
    /// Splits the <paramref name="composite"/> payload into individual items.
    /// </summary>
    /// <param name="composite">The composite payload to split.</param>
    /// <returns>A read-only list of individual items extracted from the composite payload.</returns>
    IReadOnlyList<T> Split(T composite);
}
