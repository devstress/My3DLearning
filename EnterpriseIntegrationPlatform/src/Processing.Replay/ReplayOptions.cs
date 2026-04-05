namespace EnterpriseIntegrationPlatform.Processing.Replay;

public sealed class ReplayOptions
{
    public string SourceTopic { get; set; } = string.Empty;
    public string TargetTopic { get; set; } = string.Empty;
    public int MaxMessages { get; set; } = 1000;
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// When <see langword="true"/>, messages that already carry a <c>replay-id</c>
    /// header are skipped to prevent re-replay. Defaults to <see langword="false"/>.
    /// </summary>
    public bool SkipAlreadyReplayed { get; set; }
}
