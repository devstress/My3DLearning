namespace EnterpriseIntegrationPlatform.Processing.Retry;

public record RetryResult<T>
{
    public required bool IsSucceeded { get; init; }
    public required int Attempts { get; init; }
    public Exception? LastException { get; init; }
    public T? Result { get; init; }
}
