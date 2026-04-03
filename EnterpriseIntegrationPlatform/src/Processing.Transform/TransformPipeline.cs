using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Production implementation of <see cref="ITransformPipeline"/>. Executes an ordered
/// sequence of <see cref="ITransformStep"/> instances, threading a
/// <see cref="TransformContext"/> through each step.
/// </summary>
/// <remarks>
/// <para>
/// Steps are executed in the order they are registered. The pipeline respects
/// <see cref="TransformOptions.Enabled"/> (returns input unchanged when disabled),
/// <see cref="TransformOptions.MaxPayloadSizeBytes"/> (rejects oversized payloads), and
/// <see cref="TransformOptions.StopOnStepFailure"/> (controls whether a failing step
/// halts the pipeline or is skipped with a warning).
/// </para>
/// </remarks>
public sealed class TransformPipeline : ITransformPipeline
{
    private readonly IReadOnlyList<ITransformStep> _steps;
    private readonly TransformOptions _options;
    private readonly ILogger<TransformPipeline> _logger;

    /// <summary>Initialises a new instance of <see cref="TransformPipeline"/>.</summary>
    public TransformPipeline(
        IEnumerable<ITransformStep> steps,
        IOptions<TransformOptions> options,
        ILogger<TransformPipeline> logger)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _steps = steps.ToList().AsReadOnly();
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TransformResult> ExecuteAsync(
        string payload,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (_options.MaxPayloadSizeBytes > 0 &&
            System.Text.Encoding.UTF8.GetByteCount(payload) > _options.MaxPayloadSizeBytes)
        {
            throw new InvalidOperationException(
                $"Payload size exceeds the configured maximum of {_options.MaxPayloadSizeBytes} bytes.");
        }

        if (!_options.Enabled)
        {
            _logger.LogDebug("Transform pipeline is disabled; returning input unchanged");
            return new TransformResult(payload, contentType, 0,
                new Dictionary<string, string>());
        }

        var context = new TransformContext(payload, contentType);
        var stepsApplied = 0;

        foreach (var step in _steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _logger.LogDebug("Executing transform step '{StepName}'", step.Name);
                context = await step.ExecuteAsync(context, cancellationToken);
                stepsApplied++;

                _logger.LogDebug(
                    "Transform step '{StepName}' completed; content type is now '{ContentType}'",
                    step.Name,
                    context.ContentType);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (!_options.StopOnStepFailure)
            {
                _logger.LogWarning(
                    ex,
                    "Transform step '{StepName}' failed; skipping and continuing pipeline",
                    step.Name);
            }
        }

        return new TransformResult(
            context.Payload,
            context.ContentType,
            stepsApplied,
            new Dictionary<string, string>(context.Metadata));
    }
}
