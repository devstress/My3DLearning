using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a health check result for a system component.
/// </summary>
/// <param name="ComponentName">Name of the component checked.</param>
/// <param name="Status">Health status.</param>
/// <param name="Message">Optional status message or error details.</param>
/// <param name="DurationMs">Time taken for the check in milliseconds.</param>
/// <param name="CheckedAtUtc">UTC timestamp of the check.</param>
public sealed record HealthCheckResult(
    string ComponentName,
    HealthStatus Status,
    string? Message,
    long DurationMs,
    DateTimeOffset CheckedAtUtc);
