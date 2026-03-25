using EnterpriseIntegrationPlatform.Processing.Replay;

namespace EnterpriseIntegrationPlatform.Admin.Api.Services;

/// <summary>
/// Manages DLQ message resubmission by replaying stored dead-letter envelopes
/// to a configured target topic for reprocessing.
/// </summary>
public sealed class DlqManagementService
{
    private readonly IMessageReplayer _replayer;
    private readonly ILogger<DlqManagementService> _logger;

    /// <summary>Initialises a new instance of <see cref="DlqManagementService"/>.</summary>
    public DlqManagementService(
        IMessageReplayer replayer,
        ILogger<DlqManagementService> logger)
    {
        _replayer = replayer;
        _logger = logger;
    }

    /// <summary>
    /// Replays DLQ messages that match the given filter.
    /// </summary>
    /// <param name="filter">Filter to select which DLQ messages to resubmit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ReplayResult"/> with counts of replayed, skipped, and failed messages.</returns>
    public async Task<ReplayResult> ResubmitAsync(
        ReplayFilter filter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DLQ resubmission started — correlationId={CorrelationId}, messageType={MessageType}",
            filter.CorrelationId, filter.MessageType);

        var result = await _replayer.ReplayAsync(filter, cancellationToken);

        _logger.LogInformation(
            "DLQ resubmission completed — replayed={Replayed}, failed={Failed}",
            result.ReplayedCount, result.FailedCount);

        return result;
    }
}
