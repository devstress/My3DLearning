using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// Evaluates a set of business rules against an <see cref="IntegrationEnvelope{T}"/>
/// and returns the matching actions.
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// Evaluates all enabled rules from the configured rule store against
    /// <paramref name="envelope"/> and returns the result containing matched rules and actions.
    /// </summary>
    /// <typeparam name="T">The payload type of the envelope.</typeparam>
    /// <param name="envelope">The message envelope to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="RuleEvaluationResult"/> describing the rules that matched and
    /// the actions to execute.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="envelope"/> is <see langword="null"/>.
    /// </exception>
    Task<RuleEvaluationResult> EvaluateAsync<T>(
        IntegrationEnvelope<T> envelope,
        CancellationToken cancellationToken = default);
}
