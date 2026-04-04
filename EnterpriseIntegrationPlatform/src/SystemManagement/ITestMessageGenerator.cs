using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.SystemManagement;

/// <summary>
/// Generates and publishes synthetic test messages through the platform pipeline
/// for health verification. Implements the Test Message Enterprise Integration Pattern.
/// </summary>
public interface ITestMessageGenerator
{
    /// <summary>
    /// Publishes a synthetic test message to the specified topic. The message
    /// is marked with test metadata so downstream processors can identify it.
    /// </summary>
    /// <param name="targetTopic">The topic to publish the test message to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="TestMessageResult"/> describing the outcome.</returns>
    Task<TestMessageResult> GenerateAsync(
        string targetTopic,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a synthetic test message with a custom payload.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="payload">The custom test payload.</param>
    /// <param name="targetTopic">The topic to publish the test message to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="TestMessageResult"/> describing the outcome.</returns>
    Task<TestMessageResult> GenerateAsync<T>(
        T payload,
        string targetTopic,
        CancellationToken cancellationToken = default);
}
