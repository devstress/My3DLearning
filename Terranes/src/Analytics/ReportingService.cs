using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Analytics;

/// <summary>
/// In-memory implementation of <see cref="IReportingService"/>.
/// Generates markdown reports from platform data.
/// </summary>
public sealed class ReportingService : IReportingService
{
    private readonly ConcurrentDictionary<Guid, Report> _reports = new();
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<ReportingService> _logger;

    private static readonly string[] AvailableTypes =
    [
        "PlatformOverview",
        "JourneySummary",
        "PartnerActivity",
        "EngagementReport",
        "QuoteSummary"
    ];

    public ReportingService(IAnalyticsService analyticsService, ILogger<ReportingService> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<Report> GenerateAsync(string reportType, string title, Guid generatedByUserId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reportType))
            throw new ArgumentException("Report type is required.", nameof(reportType));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (generatedByUserId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(generatedByUserId));

        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        if (!AvailableTypes.Contains(reportType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Unknown report type: {reportType}. Available types: {string.Join(", ", AvailableTypes)}", nameof(reportType));

        var totalEvents = await _analyticsService.GetTotalEventCountAsync(cancellationToken);
        var content = GenerateContent(reportType, title, totalEvents);

        var report = new Report(
            Id: Guid.NewGuid(),
            ReportType: reportType,
            Title: title,
            Content: content,
            GeneratedByUserId: generatedByUserId,
            TenantId: tenantId,
            GeneratedAtUtc: DateTimeOffset.UtcNow);

        if (!_reports.TryAdd(report.Id, report))
            throw new InvalidOperationException("Report ID conflict.");

        _logger.LogInformation("Generated {ReportType} report {ReportId}", reportType, report.Id);
        return report;
    }

    public Task<Report?> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        _reports.TryGetValue(reportId, out var report);
        return Task.FromResult(report);
    }

    public Task<IReadOnlyList<Report>> GetTenantReportsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Report> result = _reports.Values
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.GeneratedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<string>> GetAvailableReportTypesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>(AvailableTypes);

    private static string GenerateContent(string reportType, string title, int totalEvents)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();
        sb.AppendLine($"**Report Type:** {reportType}");
        sb.AppendLine($"**Generated:** {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"- Total tracked events: {totalEvents}");
        sb.AppendLine($"- Report type: {reportType}");
        sb.AppendLine();

        sb.AppendLine(reportType switch
        {
            "PlatformOverview" => "This report provides a high-level overview of platform activity including user registrations, design views, and quote requests.",
            "JourneySummary" => "This report summarises buyer journeys including stages reached, abandonment rates, and conversion to referral.",
            "PartnerActivity" => "This report details partner engagement including referral acceptance rates and quote response times.",
            "EngagementReport" => "This report tracks user engagement metrics including walkthrough completions, design edits, and village visits.",
            "QuoteSummary" => "This report summarises quoting activity including average quote values, completion rates, and partner contributions.",
            _ => "Report content generated successfully."
        });

        return sb.ToString();
    }
}
