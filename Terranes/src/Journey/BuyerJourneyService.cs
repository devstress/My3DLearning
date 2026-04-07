using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Journey;

/// <summary>
/// In-memory implementation of <see cref="IBuyerJourneyService"/>.
/// Orchestrates the buyer's end-to-end journey from browsing through to partner referral.
/// </summary>
public sealed class BuyerJourneyService : IBuyerJourneyService
{
    private readonly ConcurrentDictionary<Guid, BuyerJourney> _journeys = new();
    private readonly ILogger<BuyerJourneyService> _logger;

    public BuyerJourneyService(ILogger<BuyerJourneyService> logger) => _logger = logger;

    public Task<BuyerJourney> StartJourneyAsync(Guid buyerId, Guid? villageId = null, CancellationToken cancellationToken = default)
    {
        if (buyerId == Guid.Empty)
            throw new ArgumentException("Buyer ID is required.", nameof(buyerId));

        var now = DateTimeOffset.UtcNow;
        var journey = new BuyerJourney(
            Id: Guid.NewGuid(),
            BuyerId: buyerId,
            VillageId: villageId,
            HomeModelId: null,
            LandBlockId: null,
            SitePlacementId: null,
            QuoteRequestId: null,
            Stage: JourneyStage.Browsing,
            StartedAtUtc: now,
            UpdatedAtUtc: now);

        if (!_journeys.TryAdd(journey.Id, journey))
            throw new InvalidOperationException("Journey ID conflict.");

        _logger.LogInformation("Started buyer journey {JourneyId} for buyer {BuyerId}", journey.Id, buyerId);
        return Task.FromResult(journey);
    }

    public Task<BuyerJourney?> GetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        _journeys.TryGetValue(journeyId, out var journey);
        return Task.FromResult(journey);
    }

    public Task<IReadOnlyList<BuyerJourney>> GetBuyerJourneysAsync(Guid buyerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BuyerJourney> result = _journeys.Values
            .Where(j => j.BuyerId == buyerId)
            .OrderByDescending(j => j.StartedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<BuyerJourney> AdvanceStageAsync(Guid journeyId, JourneyStage newStage, Guid? entityId = null, CancellationToken cancellationToken = default)
    {
        if (!_journeys.TryGetValue(journeyId, out var existing))
            throw new InvalidOperationException($"Journey {journeyId} not found.");

        if (existing.Stage == JourneyStage.Completed || existing.Stage == JourneyStage.Abandoned)
            throw new InvalidOperationException($"Cannot advance a journey in stage {existing.Stage}.");

        if (newStage <= existing.Stage && newStage != JourneyStage.Abandoned)
            throw new InvalidOperationException($"Cannot go backwards from {existing.Stage} to {newStage}.");

        var updated = existing with { Stage = newStage, UpdatedAtUtc = DateTimeOffset.UtcNow };

        // Link entity based on stage
        updated = newStage switch
        {
            JourneyStage.DesignSelected when entityId.HasValue => updated with { HomeModelId = entityId },
            JourneyStage.PlacedOnLand when entityId.HasValue => updated with { LandBlockId = entityId },
            JourneyStage.Customising when entityId.HasValue => updated with { SitePlacementId = entityId },
            JourneyStage.QuoteRequested when entityId.HasValue => updated with { QuoteRequestId = entityId },
            _ => updated
        };

        _journeys[journeyId] = updated;
        _logger.LogInformation("Advanced journey {JourneyId} to stage {Stage}", journeyId, newStage);
        return Task.FromResult(updated);
    }

    public Task<BuyerJourney> AbandonJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        if (!_journeys.TryGetValue(journeyId, out var existing))
            throw new InvalidOperationException($"Journey {journeyId} not found.");

        if (existing.Stage == JourneyStage.Completed)
            throw new InvalidOperationException("Cannot abandon a completed journey.");

        var updated = existing with { Stage = JourneyStage.Abandoned, UpdatedAtUtc = DateTimeOffset.UtcNow };
        _journeys[journeyId] = updated;

        _logger.LogInformation("Abandoned journey {JourneyId}", journeyId);
        return Task.FromResult(updated);
    }

    public Task<IReadOnlyList<BuyerJourney>> GetActiveJourneysAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BuyerJourney> result = _journeys.Values
            .Where(j => j.Stage != JourneyStage.Completed && j.Stage != JourneyStage.Abandoned)
            .OrderByDescending(j => j.UpdatedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }
}
