namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a generated report.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="ReportType">Type of report (e.g. "JourneySummary", "PartnerActivity", "PlatformOverview").</param>
/// <param name="Title">Report title.</param>
/// <param name="Content">Report content (markdown or text).</param>
/// <param name="GeneratedByUserId">User who requested the report.</param>
/// <param name="TenantId">Tenant context.</param>
/// <param name="GeneratedAtUtc">UTC timestamp when the report was generated.</param>
public sealed record Report(
    Guid Id,
    string ReportType,
    string Title,
    string Content,
    Guid GeneratedByUserId,
    Guid TenantId,
    DateTimeOffset GeneratedAtUtc);
