using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class SolicitorEndpoints
{
    public static void MapSolicitorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/partners/solicitors").WithTags("Solicitors");

        group.MapPost("/register", async (RegisterSolicitorRequest request, ISolicitorService service) =>
        {
            var created = await service.RegisterAsync(request.Partner, request.Profile);
            return Results.Created($"/api/partners/solicitors/{created.PartnerId}", created);
        }).WithName("RegisterSolicitor");

        group.MapGet("/{partnerId:guid}", async (Guid partnerId, ISolicitorService service) =>
        {
            var profile = await service.GetProfileAsync(partnerId);
            return profile is not null ? Results.Ok(profile) : Results.NotFound();
        }).WithName("GetSolicitorProfile");

        group.MapGet("/search", async (bool? conveyancing, bool? contractReview, ISolicitorService service) =>
        {
            var results = await service.FindSolicitorsAsync(conveyancing, contractReview);
            return Results.Ok(results);
        }).WithName("SearchSolicitors");

        group.MapPost("/{partnerId:guid}/quotes/{quoteRequestId:guid}", async (Guid partnerId, Guid quoteRequestId, string serviceType, ISolicitorService service) =>
        {
            var response = await service.RequestQuoteAsync(partnerId, quoteRequestId, serviceType);
            return Results.Ok(response);
        }).WithName("RequestSolicitorQuote");
    }
}

public sealed record RegisterSolicitorRequest(Partner Partner, SolicitorProfile Profile);