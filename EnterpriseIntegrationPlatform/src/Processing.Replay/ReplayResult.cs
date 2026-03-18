namespace EnterpriseIntegrationPlatform.Processing.Replay;

public record ReplayResult
{
    public required int ReplayedCount { get; init; }
    public required int SkippedCount { get; init; }
    public required int FailedCount { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
}
