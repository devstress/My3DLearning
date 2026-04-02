namespace EnterpriseIntegrationPlatform.Processing.ScatterGather;

/// <summary>
/// Represents a single gathered response from a recipient.
/// </summary>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
/// <param name="Recipient">The topic name of the recipient that produced this response.</param>
/// <param name="Payload">The response payload.</param>
/// <param name="ReceivedAt">The timestamp when the response was received.</param>
/// <param name="IsSuccess">Whether the recipient processed the request successfully.</param>
/// <param name="ErrorMessage">An optional error message when <paramref name="IsSuccess"/> is <see langword="false"/>.</param>
public sealed record GatherResponse<TResponse>(
    string Recipient,
    TResponse Payload,
    DateTimeOffset ReceivedAt,
    bool IsSuccess,
    string? ErrorMessage);
