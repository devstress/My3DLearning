namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// Persistent store for <see cref="BusinessRule"/> instances.
/// Supports runtime registration, removal, and retrieval of rules.
/// </summary>
public interface IRuleStore
{
    /// <summary>
    /// Returns all registered rules, sorted by <see cref="BusinessRule.Priority"/> ascending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Priority-sorted list of all rules.</returns>
    Task<IReadOnlyList<BusinessRule>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single rule by name.
    /// </summary>
    /// <param name="name">The unique rule name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rule if found; <see langword="null"/> otherwise.</returns>
    Task<BusinessRule?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or replaces a rule. If a rule with the same name exists, it is overwritten.
    /// </summary>
    /// <param name="rule">The rule to register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddOrUpdateAsync(BusinessRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a rule by name.
    /// </summary>
    /// <param name="name">The unique rule name to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> if the rule was found and removed; <see langword="false"/> otherwise.</returns>
    Task<bool> RemoveAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Returns the total number of registered rules.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
