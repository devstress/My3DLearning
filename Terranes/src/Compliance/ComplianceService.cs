using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Compliance;

/// <summary>
/// Rule-based implementation of <see cref="IComplianceService"/>.
/// Checks building regulation compliance per jurisdiction using configurable rules.
/// </summary>
public sealed class ComplianceService : IComplianceService
{
    private readonly ConcurrentDictionary<Guid, ComplianceResult> _store = new();
    private readonly ISitePlacementService _sitePlacementService;
    private readonly IHomeModelService _homeModelService;
    private readonly ILandBlockService _landBlockService;
    private readonly ILogger<ComplianceService> _logger;

    public ComplianceService(
        ISitePlacementService sitePlacementService,
        IHomeModelService homeModelService,
        ILandBlockService landBlockService,
        ILogger<ComplianceService> logger)
    {
        _sitePlacementService = sitePlacementService;
        _homeModelService = homeModelService;
        _landBlockService = landBlockService;
        _logger = logger;
    }

    public async Task<ComplianceResult> CheckAsync(Guid sitePlacementId, string jurisdiction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jurisdiction))
            throw new ArgumentException("Jurisdiction is required.", nameof(jurisdiction));

        var placement = await _sitePlacementService.GetByIdAsync(sitePlacementId, cancellationToken)
            ?? throw new InvalidOperationException($"Site placement {sitePlacementId} not found.");

        var model = await _homeModelService.GetByIdAsync(placement.HomeModelId, cancellationToken)
            ?? throw new InvalidOperationException($"Home model {placement.HomeModelId} not found.");

        var block = await _landBlockService.GetByIdAsync(placement.LandBlockId, cancellationToken)
            ?? throw new InvalidOperationException($"Land block {placement.LandBlockId} not found.");

        var violations = RunComplianceRules(model, block, placement, jurisdiction);

        var outcome = violations.Count switch
        {
            0 => ComplianceOutcome.Compliant,
            _ when violations.Any(v => v.Severity == "Critical") => ComplianceOutcome.NonCompliant,
            _ when violations.Any(v => v.Severity == "Warning") => ComplianceOutcome.ConditionallyCompliant,
            _ => ComplianceOutcome.RequiresReview
        };

        var result = new ComplianceResult(
            Guid.NewGuid(),
            sitePlacementId,
            outcome,
            violations,
            jurisdiction,
            DateTimeOffset.UtcNow);

        _store.TryAdd(result.Id, result);
        _logger.LogInformation("Compliance check {CheckId}: {Outcome} for placement {PlacementId} in {Jurisdiction} ({ViolationCount} violations)",
            result.Id, result.Outcome, sitePlacementId, jurisdiction, violations.Count);
        return result;
    }

    public Task<ComplianceResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var result);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<ComplianceResult>> GetBySitePlacementAsync(Guid sitePlacementId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ComplianceResult> results = _store.Values
            .Where(r => r.SitePlacementId == sitePlacementId)
            .OrderByDescending(r => r.CheckedAtUtc)
            .ToList();
        return Task.FromResult(results);
    }

    private static List<ComplianceViolation> RunComplianceRules(HomeModel model, LandBlock block, Contracts.Models.SitePlacement placement, string jurisdiction)
    {
        var violations = new List<ComplianceViolation>();

        // Rule 1: Maximum site coverage (typically 60% for residential)
        var footprint = model.FloorAreaSquareMetres * placement.ScaleFactor * placement.ScaleFactor;
        var coverageRatio = footprint / block.AreaSquareMetres;
        if (coverageRatio > 0.6)
        {
            violations.Add(new ComplianceViolation(
                "BCA-COVERAGE-001",
                $"Site coverage {coverageRatio:P0} exceeds maximum 60% for {block.Zoning} zoning.",
                "Critical"));
        }

        // Rule 2: Minimum setback from boundary (1.5m standard residential)
        const double minSetbackMetres = 1.5;
        if (placement.OffsetXMetres < minSetbackMetres)
        {
            violations.Add(new ComplianceViolation(
                "BCA-SETBACK-001",
                $"Front setback {placement.OffsetXMetres:F1}m is less than minimum {minSetbackMetres}m.",
                "Critical"));
        }

        // Rule 3: Minimum lot size for dwelling (varies by zoning)
        var minLotSize = block.Zoning switch
        {
            ZoningType.Residential => 300.0,
            ZoningType.ResidentialMediumDensity => 200.0,
            ZoningType.ResidentialHighDensity => 150.0,
            ZoningType.RuralResidential => 2000.0,
            _ => 300.0
        };

        if (block.AreaSquareMetres < minLotSize)
        {
            violations.Add(new ComplianceViolation(
                "BCA-LOTSIZE-001",
                $"Lot size {block.AreaSquareMetres:F0}m² is below minimum {minLotSize:F0}m² for {block.Zoning} zoning.",
                "Critical"));
        }

        // Rule 4: Frontage minimum (typically 10m for standard residential)
        if (block.Zoning is ZoningType.Residential or ZoningType.RuralResidential && block.FrontageMetres < 10.0)
        {
            violations.Add(new ComplianceViolation(
                "BCA-FRONTAGE-001",
                $"Frontage {block.FrontageMetres:F1}m is below minimum 10m for {block.Zoning} zoning.",
                "Warning"));
        }

        return violations;
    }
}
