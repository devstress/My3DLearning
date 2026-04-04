namespace EnterpriseIntegrationPlatform.Gateway.Api;

/// <summary>
/// Response from the Messaging Gateway.
/// </summary>
public sealed record GatewayResponse
{
    /// <summary>The correlation ID assigned to this request.</summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>Whether the operation completed successfully.</summary>
    public required bool Success { get; init; }

    /// <summary>HTTP status code returned by the downstream service, if applicable.</summary>
    public int? StatusCode { get; init; }

    /// <summary>Error message when <see cref="Success"/> is <c>false</c>.</summary>
    public string? Error { get; init; }
}

/// <summary>
/// Response from the Messaging Gateway that includes a typed response payload.
/// </summary>
/// <typeparam name="T">The type of the response payload.</typeparam>
public sealed record GatewayResponse<T>
{
    /// <summary>The correlation ID assigned to this request.</summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>Whether the operation completed successfully.</summary>
    public required bool Success { get; init; }

    /// <summary>HTTP status code returned by the downstream service, if applicable.</summary>
    public int? StatusCode { get; init; }

    /// <summary>Error message when <see cref="Success"/> is <c>false</c>.</summary>
    public string? Error { get; init; }

    /// <summary>The response payload from the downstream service.</summary>
    public T? Payload { get; init; }
}
