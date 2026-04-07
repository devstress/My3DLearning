using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;

namespace Terranes.Platform.Api.Endpoints;

public static class ReferralEndpoints
{
    public static void MapReferralEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/referrals").WithTags("Referrals");

        group.MapPost("/", async (Guid journeyId, Guid partnerId, PartnerCategory category, string buyerName, string? notes, IReferralService service) =>
        {
            var referral = await service.CreateReferralAsync(journeyId, partnerId, category, buyerName, notes);
            return Results.Created($"/api/referrals/{referral.Id}", referral);
        }).WithName("CreateReferral");

        group.MapGet("/{referralId:guid}", async (Guid referralId, IReferralService service) =>
        {
            var referral = await service.GetReferralAsync(referralId);
            return referral is not null ? Results.Ok(referral) : Results.NotFound();
        }).WithName("GetReferral");

        group.MapGet("/journey/{journeyId:guid}", async (Guid journeyId, IReferralService service) =>
        {
            var referrals = await service.GetReferralsForJourneyAsync(journeyId);
            return Results.Ok(referrals);
        }).WithName("GetJourneyReferrals");

        group.MapGet("/partner/{partnerId:guid}", async (Guid partnerId, IReferralService service) =>
        {
            var referrals = await service.GetReferralsForPartnerAsync(partnerId);
            return Results.Ok(referrals);
        }).WithName("GetPartnerReferrals");

        group.MapPut("/{referralId:guid}/status", async (Guid referralId, ReferralStatus status, IReferralService service) =>
        {
            var referral = await service.UpdateStatusAsync(referralId, status);
            return Results.Ok(referral);
        }).WithName("UpdateReferralStatus");
    }
}
