using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Orchestrates the buyer's end-to-end journey through the Terranes platform.
/// </summary>
public interface IBuyerJourneyService
{
    /// <summary>Starts a new buyer journey.</summary>
    Task<BuyerJourney> StartJourneyAsync(Guid buyerId, Guid? villageId = null, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a journey by its identifier.</summary>
    Task<BuyerJourney?> GetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all journeys for a buyer.</summary>
    Task<IReadOnlyList<BuyerJourney>> GetBuyerJourneysAsync(Guid buyerId, CancellationToken cancellationToken = default);

    /// <summary>Advances the journey to the next stage, linking an entity as appropriate.</summary>
    Task<BuyerJourney> AdvanceStageAsync(Guid journeyId, JourneyStage newStage, Guid? entityId = null, CancellationToken cancellationToken = default);

    /// <summary>Abandons a journey.</summary>
    Task<BuyerJourney> AbandonJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default);

    /// <summary>Gets all active (non-abandoned, non-completed) journeys.</summary>
    Task<IReadOnlyList<BuyerJourney>> GetActiveJourneysAsync(CancellationToken cancellationToken = default);
}
