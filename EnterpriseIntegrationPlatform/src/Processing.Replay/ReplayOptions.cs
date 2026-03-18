namespace EnterpriseIntegrationPlatform.Processing.Replay;

public sealed class ReplayOptions
{
    public string SourceTopic { get; set; } = string.Empty;
    public string TargetTopic { get; set; } = string.Empty;
    public int MaxMessages { get; set; } = 1000;
    public int BatchSize { get; set; } = 100;
}
