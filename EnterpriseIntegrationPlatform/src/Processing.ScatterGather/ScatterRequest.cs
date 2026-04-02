namespace EnterpriseIntegrationPlatform.Processing.ScatterGather;

/// <summary>
/// Represents a scatter request to be broadcast to multiple recipient topics.
/// </summary>
/// <typeparam name="TRequest">The type of the request payload.</typeparam>
/// <param name="CorrelationId">Unique identifier correlating all scatter/gather messages.</param>
/// <param name="Payload">The request payload to broadcast.</param>
/// <param name="Recipients">Ordered list of recipient topic names to scatter the request to.</param>
public sealed record ScatterRequest<TRequest>(
    Guid CorrelationId,
    TRequest Payload,
    IReadOnlyList<string> Recipients);
