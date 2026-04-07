using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Result of a building compliance check for a site placement in a given jurisdiction.
/// </summary>
public sealed record ComplianceResult(
    Guid Id,
    Guid SitePlacementId,
    ComplianceOutcome Outcome,
    IReadOnlyList<ComplianceViolation> Violations,
    string Jurisdiction,
    DateTimeOffset CheckedAtUtc);
