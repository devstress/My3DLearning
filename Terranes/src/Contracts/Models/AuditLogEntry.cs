namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a structured audit log entry.
/// </summary>
/// <param name="Id">Unique identifier for the log entry.</param>
/// <param name="Action">Action that was performed (e.g. "CreateHomeModel").</param>
/// <param name="EntityType">Type of entity acted upon.</param>
/// <param name="EntityId">Identifier of the entity.</param>
/// <param name="UserId">User who performed the action.</param>
/// <param name="TenantId">Tenant context.</param>
/// <param name="Details">JSON details of the action.</param>
/// <param name="TimestampUtc">UTC timestamp.</param>
public sealed record AuditLogEntry(
    Guid Id,
    string Action,
    string EntityType,
    Guid EntityId,
    Guid UserId,
    Guid TenantId,
    string? Details,
    DateTimeOffset TimestampUtc);
