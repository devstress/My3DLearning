using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Journey;

/// <summary>
/// In-memory implementation of <see cref="IReferralService"/>.
/// Manages referrals of qualified buyers to partners.
/// </summary>
public sealed class ReferralService : IReferralService
{
    private readonly ConcurrentDictionary<Guid, PartnerReferral> _referrals = new();
    private readonly ILogger<ReferralService> _logger;

    public ReferralService(ILogger<ReferralService> logger) => _logger = logger;

    public Task<PartnerReferral> CreateReferralAsync(Guid journeyId, Guid partnerId, PartnerCategory category, string buyerName, string? notes = null, CancellationToken cancellationToken = default)
    {
        if (journeyId == Guid.Empty)
            throw new ArgumentException("Journey ID is required.", nameof(journeyId));

        if (partnerId == Guid.Empty)
            throw new ArgumentException("Partner ID is required.", nameof(partnerId));

        if (string.IsNullOrWhiteSpace(buyerName))
            throw new ArgumentException("Buyer name is required.", nameof(buyerName));

        var referral = new PartnerReferral(
            Id: Guid.NewGuid(),
            JourneyId: journeyId,
            PartnerId: partnerId,
            PartnerCategory: category,
            Status: ReferralStatus.Pending,
            BuyerName: buyerName,
            Notes: notes,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            RespondedAtUtc: null);

        if (!_referrals.TryAdd(referral.Id, referral))
            throw new InvalidOperationException("Referral ID conflict.");

        _logger.LogInformation("Created referral {ReferralId} for journey {JourneyId} to partner {PartnerId}", referral.Id, journeyId, partnerId);
        return Task.FromResult(referral);
    }

    public Task<PartnerReferral?> GetReferralAsync(Guid referralId, CancellationToken cancellationToken = default)
    {
        _referrals.TryGetValue(referralId, out var referral);
        return Task.FromResult(referral);
    }

    public Task<IReadOnlyList<PartnerReferral>> GetReferralsForJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PartnerReferral> result = _referrals.Values
            .Where(r => r.JourneyId == journeyId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<PartnerReferral>> GetReferralsForPartnerAsync(Guid partnerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PartnerReferral> result = _referrals.Values
            .Where(r => r.PartnerId == partnerId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<PartnerReferral> UpdateStatusAsync(Guid referralId, ReferralStatus newStatus, CancellationToken cancellationToken = default)
    {
        if (!_referrals.TryGetValue(referralId, out var existing))
            throw new InvalidOperationException($"Referral {referralId} not found.");

        if (existing.Status != ReferralStatus.Pending && existing.Status != ReferralStatus.Sent)
            throw new InvalidOperationException($"Cannot update referral in status {existing.Status}.");

        var updated = existing with
        {
            Status = newStatus,
            RespondedAtUtc = newStatus is ReferralStatus.Accepted or ReferralStatus.Declined
                ? DateTimeOffset.UtcNow
                : existing.RespondedAtUtc
        };

        _referrals[referralId] = updated;
        _logger.LogInformation("Updated referral {ReferralId} status to {Status}", referralId, newStatus);
        return Task.FromResult(updated);
    }
}
