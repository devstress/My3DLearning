namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// The result of running a <see cref="ITransformPipeline"/>.
/// </summary>
/// <param name="Payload">The fully-transformed payload string.</param>
/// <param name="ContentType">The content type of the transformed payload.</param>
/// <param name="StepsApplied">Number of transform steps that were executed.</param>
/// <param name="Metadata">Accumulated metadata from all transform steps.</param>
public sealed record TransformResult(
    string Payload,
    string ContentType,
    int StepsApplied,
    IReadOnlyDictionary<string, string> Metadata);
