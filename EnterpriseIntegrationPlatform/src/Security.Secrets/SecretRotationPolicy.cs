namespace EnterpriseIntegrationPlatform.Security.Secrets;

/// <summary>
/// Defines the rotation schedule and behavior for a secret.
/// </summary>
/// <param name="RotationInterval">The interval between automatic rotations.</param>
/// <param name="AutoRotate">Whether rotation should occur automatically when the interval elapses.</param>
/// <param name="RotateBeforeExpiry">
/// How far before expiry to trigger rotation.
/// When set, rotation occurs when the secret's remaining lifetime is less than this duration.
/// </param>
/// <param name="NotifyOnRotation">Whether to emit an audit event when rotation occurs.</param>
public sealed record SecretRotationPolicy(
    TimeSpan RotationInterval,
    bool AutoRotate = true,
    TimeSpan? RotateBeforeExpiry = null,
    bool NotifyOnRotation = true);
