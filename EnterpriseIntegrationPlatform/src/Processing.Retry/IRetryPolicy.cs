namespace EnterpriseIntegrationPlatform.Processing.Retry;

public interface IRetryPolicy
{
    Task<RetryResult<T>> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct);
    Task<RetryResult<bool>> ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct);
}
