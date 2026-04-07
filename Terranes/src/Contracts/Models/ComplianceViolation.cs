namespace Terranes.Contracts.Models;

/// <summary>
/// Represents an individual building regulation violation found during a compliance check.
/// </summary>
public sealed record ComplianceViolation(
    string RuleCode,
    string Description,
    string Severity);
