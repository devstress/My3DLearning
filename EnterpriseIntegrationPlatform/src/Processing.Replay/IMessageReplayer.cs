namespace EnterpriseIntegrationPlatform.Processing.Replay;

public interface IMessageReplayer
{
    Task<ReplayResult> ReplayAsync(ReplayFilter filter, CancellationToken ct);
}
