namespace EnterpriseIntegrationPlatform.Processing.DeadLetter;

public sealed class DeadLetterOptions
{
    public string DeadLetterTopic { get; set; } = string.Empty;
    public int MaxRetryAttempts { get; set; } = 3;
    public string? Source { get; set; }
    public string MessageType { get; set; } = "DeadLetter";
}
