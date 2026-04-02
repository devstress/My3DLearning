using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.ScatterGather;

/// <summary>
/// Production implementation of the Scatter-Gather Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// <para>
/// The scatter phase publishes an <see cref="IntegrationEnvelope{TRequest}"/> to each
/// recipient topic via <see cref="IMessageBrokerProducer"/>. The gather phase waits
/// for responses submitted through <see cref="SubmitResponseAsync"/> using a bounded
/// <see cref="Channel{T}"/>. The operation completes when all recipients have responded
/// or the configured <see cref="ScatterGatherOptions.TimeoutMs"/> expires, whichever
/// comes first.
/// </para>
/// <para>
/// This class is thread-safe. Multiple concurrent scatter-gather operations are supported
/// and isolated by <see cref="ScatterRequest{TRequest}.CorrelationId"/>.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">The type of the request payload.</typeparam>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
public sealed class ScatterGatherer<TRequest, TResponse> : IScatterGatherer<TRequest, TResponse>
{
    private readonly IMessageBrokerProducer _producer;
    private readonly ScatterGatherOptions _options;
    private readonly ILogger<ScatterGatherer<TRequest, TResponse>> _logger;

    private readonly ConcurrentDictionary<Guid, GatherContext> _activeGathers = new();

    /// <summary>Initialises a new instance of <see cref="ScatterGatherer{TRequest,TResponse}"/>.</summary>
    /// <param name="producer">Message broker producer for publishing scatter requests.</param>
    /// <param name="options">Scatter-gather configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public ScatterGatherer(
        IMessageBrokerProducer producer,
        IOptions<ScatterGatherOptions> options,
        ILogger<ScatterGatherer<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ScatterGatherResult<TResponse>> ScatterGatherAsync(
        ScatterRequest<TRequest> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Recipients is null || request.Recipients.Count == 0)
        {
            return new ScatterGatherResult<TResponse>(
                request.CorrelationId,
                [],
                TimedOut: false,
                Duration: TimeSpan.Zero);
        }

        if (request.Recipients.Count > _options.MaxRecipients)
        {
            throw new ArgumentException(
                $"Recipient count {request.Recipients.Count} exceeds the configured maximum of {_options.MaxRecipients}.",
                nameof(request));
        }

        var channel = Channel.CreateBounded<GatherResponse<TResponse>>(
            new BoundedChannelOptions(request.Recipients.Count)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            });

        var context = new GatherContext(channel, request.Recipients.Count);

        if (!_activeGathers.TryAdd(request.CorrelationId, context))
        {
            throw new InvalidOperationException(
                $"A scatter-gather operation with CorrelationId {request.CorrelationId} is already in progress.");
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await ScatterAsync(request, cancellationToken).ConfigureAwait(false);

            var responses = await GatherAsync(
                request.CorrelationId,
                context,
                cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();

            var timedOut = responses.Count < request.Recipients.Count;

            _logger.LogDebug(
                "Scatter-gather {CorrelationId} completed in {Duration}ms — " +
                "{ResponseCount}/{RecipientCount} responses, timedOut={TimedOut}",
                request.CorrelationId,
                stopwatch.ElapsedMilliseconds,
                responses.Count,
                request.Recipients.Count,
                timedOut);

            return new ScatterGatherResult<TResponse>(
                request.CorrelationId,
                responses,
                timedOut,
                stopwatch.Elapsed);
        }
        finally
        {
            _activeGathers.TryRemove(request.CorrelationId, out _);
        }
    }

    /// <summary>
    /// Submits a response from a recipient for an active scatter-gather operation.
    /// </summary>
    /// <param name="correlationId">The correlation identifier of the scatter-gather operation.</param>
    /// <param name="response">The gathered response to submit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> if the response was accepted;
    /// <see langword="false"/> if no active gather exists for the given correlation identifier.
    /// </returns>
    public async Task<bool> SubmitResponseAsync(
        Guid correlationId,
        GatherResponse<TResponse> response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!_activeGathers.TryGetValue(correlationId, out var context))
        {
            _logger.LogWarning(
                "Received response for unknown or completed scatter-gather {CorrelationId} from {Recipient}",
                correlationId,
                response.Recipient);
            return false;
        }

        try
        {
            await context.Channel.Writer.WriteAsync(response, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Response received for {CorrelationId} from {Recipient} (success={IsSuccess})",
                correlationId,
                response.Recipient,
                response.IsSuccess);

            return true;
        }
        catch (ChannelClosedException)
        {
            return false;
        }
    }

    private async Task ScatterAsync(
        ScatterRequest<TRequest> request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Scattering request {CorrelationId} to {RecipientCount} recipient(s)",
            request.CorrelationId,
            request.Recipients.Count);

        var envelope = IntegrationEnvelope<TRequest>.Create(
            request.Payload,
            source: "ScatterGatherer",
            messageType: typeof(TRequest).Name,
            correlationId: request.CorrelationId);

        var publishTasks = new Task[request.Recipients.Count];
        for (var i = 0; i < request.Recipients.Count; i++)
        {
            publishTasks[i] = _producer.PublishAsync(envelope, request.Recipients[i], cancellationToken);
        }

        await Task.WhenAll(publishTasks).ConfigureAwait(false);

        _logger.LogDebug(
            "Scatter phase complete for {CorrelationId} — published to {RecipientCount} topic(s)",
            request.CorrelationId,
            request.Recipients.Count);
    }

    private async Task<IReadOnlyList<GatherResponse<TResponse>>> GatherAsync(
        Guid correlationId,
        GatherContext context,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(_options.TimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCts.Token,
            cancellationToken);

        var responses = new List<GatherResponse<TResponse>>(context.ExpectedCount);

        try
        {
            await foreach (var response in context.Channel.Reader.ReadAllAsync(linkedCts.Token).ConfigureAwait(false))
            {
                responses.Add(response);

                if (responses.Count >= context.ExpectedCount)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Scatter-gather {CorrelationId} timed out after {TimeoutMs}ms with {ResponseCount}/{ExpectedCount} responses",
                correlationId,
                _options.TimeoutMs,
                responses.Count,
                context.ExpectedCount);
        }

        context.Channel.Writer.TryComplete();
        return responses;
    }

    private sealed record GatherContext(
        Channel<GatherResponse<TResponse>> Channel,
        int ExpectedCount);
}
