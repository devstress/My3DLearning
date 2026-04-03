using System.Collections.Concurrent;

namespace EnterpriseIntegrationPlatform.RuleEngine;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IRuleStore"/>.
/// Suitable for development, testing, and scenarios where rules are loaded from configuration.
/// </summary>
public sealed class InMemoryRuleStore : IRuleStore
{
    private readonly ConcurrentDictionary<string, BusinessRule> _rules = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<IReadOnlyList<BusinessRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<BusinessRule> sorted = [.. _rules.Values.OrderBy(r => r.Priority)];
        return Task.FromResult(sorted);
    }

    /// <inheritdoc />
    public Task<BusinessRule?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        cancellationToken.ThrowIfCancellationRequested();

        _rules.TryGetValue(name, out var rule);
        return Task.FromResult(rule);
    }

    /// <inheritdoc />
    public Task AddOrUpdateAsync(BusinessRule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);
        cancellationToken.ThrowIfCancellationRequested();

        _rules[rule.Name] = rule;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_rules.TryRemove(name, out _));
    }

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_rules.Count);
    }
}
