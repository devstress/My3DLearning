using System.Collections.Concurrent;
using System.Diagnostics;
using EnterpriseIntegrationPlatform.Contracts;
using EnterpriseIntegrationPlatform.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.RequestReply;

/// <summary>
/// Production implementation of the Request-Reply Enterprise Integration Pattern.
/// </summary>
/// <remarks>
/// <para>
/// Publishes a request <see cref="IntegrationEnvelope{TRequest}"/> with
/// <see cref="IntegrationEnvelope{T}.ReplyTo"/> set to the configured reply topic,
/// then subscribes to that topic and waits for a reply whose
/// <see cref="IntegrationEnvelope{T}.CorrelationId"/> matches the request.
/// </para>
/// <para>
/// This is the async messaging equivalent of HTTP request-response and replaces
/// the BizTalk solicit-response send port pattern.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">The type of the request payload.</typeparam>
/// <typeparam name="TResponse">The type of the expected response payload.</typeparam>
public sealed class RequestReplyCorrelator<TRequest, TResponse> : IRequestReplyCorrelator<TRequest, TResponse>
{
    private readonly IMessageBrokerProducer _producer;
    private readonly IMessageBrokerConsumer _consumer;
    private readonly RequestReplyOptions _options;
    private readonly ILogger<RequestReplyCorrelator<TRequest, TResponse>> _logger;

    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<IntegrationEnvelope<TResponse>>> _pendingRequests = new();

    /// <summary>Initialises a new instance of <see cref="RequestReplyCorrelator{TRequest,TResponse}"/>.</summary>
    public RequestReplyCorrelator(
        IMessageBrokerProducer producer,
        IMessageBrokerConsumer consumer,
        IOptions<RequestReplyOptions> options,
        ILogger<RequestReplyCorrelator<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _producer = producer;
        _consumer = consumer;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RequestReplyResult<TResponse>> SendAndReceiveAsync(
        RequestReplyMessage<TRequest> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RequestTopic))
            throw new ArgumentException("RequestTopic must not be empty.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.ReplyTopic))
            throw new ArgumentException("ReplyTopic must not be empty.", nameof(request));

        var correlationId = request.CorrelationId ?? Guid.NewGuid();
        var tcs = new TaskCompletionSource<IntegrationEnvelope<TResponse>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_pendingRequests.TryAdd(correlationId, tcs))
        {
            throw new InvalidOperationException(
                $"A request-reply operation with CorrelationId {correlationId} is already in progress.");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Start listening for the reply before publishing the request
            using var subscribeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var subscribeTask = _consumer.SubscribeAsync<TResponse>(
                request.ReplyTopic,
                _options.ConsumerGroup,
                envelope => HandleReply(envelope),
                subscribeCts.Token);

            // Publish the request with ReplyTo set
            var requestEnvelope = IntegrationEnvelope<TRequest>.Create(
                request.Payload,
                source: request.Source,
                messageType: request.MessageType,
                correlationId: correlationId) with
            {
                ReplyTo = request.ReplyTopic,
                Intent = MessageIntent.Command,
            };

            await _producer.PublishAsync(requestEnvelope, request.RequestTopic, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "Request {CorrelationId} published to {RequestTopic}, waiting for reply on {ReplyTopic}",
                correlationId, request.RequestTopic, request.ReplyTopic);

            // Wait for reply or timeout
            using var timeoutCts = new CancellationTokenSource(_options.TimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                timeoutCts.Token, cancellationToken);

            try
            {
                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(-1, linkedCts.Token))
                    .ConfigureAwait(false);

                if (completedTask == tcs.Task)
                {
                    var reply = await tcs.Task.ConfigureAwait(false);
                    stopwatch.Stop();

                    _logger.LogDebug(
                        "Reply received for {CorrelationId} in {Duration}ms",
                        correlationId, stopwatch.ElapsedMilliseconds);

                    return new RequestReplyResult<TResponse>(
                        correlationId, reply, TimedOut: false, stopwatch.Elapsed);
                }
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Timeout — fall through to return timed-out result
            }

            // Cancel subscription
            await subscribeCts.CancelAsync().ConfigureAwait(false);

            stopwatch.Stop();

            _logger.LogWarning(
                "Request-reply {CorrelationId} timed out after {TimeoutMs}ms",
                correlationId, _options.TimeoutMs);

            return new RequestReplyResult<TResponse>(
                correlationId, Reply: null, TimedOut: true, stopwatch.Elapsed);
        }
        finally
        {
            _pendingRequests.TryRemove(correlationId, out _);
        }
    }

    private Task HandleReply(IntegrationEnvelope<TResponse> envelope)
    {
        if (_pendingRequests.TryGetValue(envelope.CorrelationId, out var tcs))
        {
            tcs.TrySetResult(envelope);

            _logger.LogDebug(
                "Correlated reply {MessageId} to pending request {CorrelationId}",
                envelope.MessageId, envelope.CorrelationId);
        }
        else
        {
            _logger.LogWarning(
                "Received reply {MessageId} with CorrelationId {CorrelationId} but no pending request found — " +
                "possible duplicate or late arrival after timeout",
                envelope.MessageId, envelope.CorrelationId);
        }

        return Task.CompletedTask;
    }
}
