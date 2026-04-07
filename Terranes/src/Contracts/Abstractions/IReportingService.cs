using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Generates reports and exports for platform data.
/// </summary>
public interface IReportingService
{
    /// <summary>Generates a report.</summary>
    Task<Report> GenerateAsync(string reportType, string title, Guid generatedByUserId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Gets a report by ID.</summary>
    Task<Report?> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default);

    /// <summary>Gets all reports for a tenant.</summary>
    Task<IReadOnlyList<Report>> GetTenantReportsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Gets available report types.</summary>
    Task<IReadOnlyList<string>> GetAvailableReportTypesAsync(CancellationToken cancellationToken = default);
}
