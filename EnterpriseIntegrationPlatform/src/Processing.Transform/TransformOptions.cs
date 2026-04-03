namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Configuration options for <see cref="TransformPipeline"/>.
/// Bind from the <c>TransformPipeline</c> configuration section.
/// </summary>
public sealed class TransformOptions
{
    /// <summary>
    /// Whether the transform pipeline is enabled. When <c>false</c>, the pipeline
    /// returns the input payload unchanged.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum payload size in bytes that the pipeline will accept.
    /// Payloads exceeding this limit cause the pipeline to throw
    /// <see cref="InvalidOperationException"/>.
    /// A value of <c>0</c> means unlimited.
    /// </summary>
    public long MaxPayloadSizeBytes { get; set; }

    /// <summary>
    /// Whether to halt the pipeline on the first step failure (<c>true</c>) or
    /// skip the failing step and continue (<c>false</c>). Default is <c>true</c>.
    /// </summary>
    public bool StopOnStepFailure { get; set; } = true;
}
