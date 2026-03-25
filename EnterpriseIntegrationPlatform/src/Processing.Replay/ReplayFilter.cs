namespace EnterpriseIntegrationPlatform.Processing.Replay;

public record ReplayFilter
{
    public Guid? CorrelationId { get; init; }
    public string? MessageType { get; init; }
    public DateTimeOffset? FromTimestamp { get; init; }
    public DateTimeOffset? ToTimestamp { get; init; }
}
