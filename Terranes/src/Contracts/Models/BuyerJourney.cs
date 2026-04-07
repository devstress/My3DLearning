using Terranes.Contracts.Enums;

namespace Terranes.Contracts.Models;

/// <summary>
/// Represents a buyer's end-to-end journey through the Terranes platform — from browsing
/// the virtual village through to partner referral.
/// </summary>
/// <param name="Id">Unique identifier for this journey.</param>
/// <param name="BuyerId">Identifier of the buyer (platform user).</param>
/// <param name="VillageId">Optional village the buyer is browsing.</param>
/// <param name="HomeModelId">Optional selected home model.</param>
/// <param name="LandBlockId">Optional land block for test-fit.</param>
/// <param name="SitePlacementId">Optional site placement result.</param>
/// <param name="QuoteRequestId">Optional linked quote request.</param>
/// <param name="Stage">Current stage of the journey.</param>
/// <param name="StartedAtUtc">UTC timestamp when the journey began.</param>
/// <param name="UpdatedAtUtc">UTC timestamp of the last stage change.</param>
public sealed record BuyerJourney(
    Guid Id,
    Guid BuyerId,
    Guid? VillageId,
    Guid? HomeModelId,
    Guid? LandBlockId,
    Guid? SitePlacementId,
    Guid? QuoteRequestId,
    JourneyStage Stage,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset UpdatedAtUtc);
