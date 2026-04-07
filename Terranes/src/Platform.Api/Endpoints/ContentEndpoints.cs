using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Platform.Api.Endpoints;

public static class ContentEndpoints
{
    public static void MapContentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/content").WithTags("User Content");

        group.MapPost("/posts", async (ContentPost post, IContentService service) =>
        {
            var created = await service.CreatePostAsync(post);
            return Results.Created($"/api/content/posts/{created.Id}", created);
        }).WithName("CreateContentPost");

        group.MapGet("/posts/{postId:guid}", async (Guid postId, IContentService service) =>
        {
            var post = await service.GetPostAsync(postId);
            return post is not null ? Results.Ok(post) : Results.NotFound();
        }).WithName("GetContentPost");

        group.MapGet("/posts", async (ContentPostStatus? status, Guid? userId, IContentService service) =>
        {
            var results = await service.SearchPostsAsync(status, userId);
            return Results.Ok(results);
        }).WithName("SearchContentPosts");

        group.MapPost("/posts/{postId:guid}/publish", async (Guid postId, IContentService service) =>
        {
            var published = await service.PublishPostAsync(postId);
            return Results.Ok(published);
        }).WithName("PublishContentPost");

        group.MapPost("/posts/{postId:guid}/ratings", async (Guid postId, ContentRating rating, IContentService service) =>
        {
            var ratingWithPost = rating with { ContentPostId = postId };
            var created = await service.RatePostAsync(ratingWithPost);
            return Results.Created($"/api/content/posts/{postId}/ratings/{created.Id}", created);
        }).WithName("RateContentPost");

        group.MapGet("/posts/{postId:guid}/ratings", async (Guid postId, IContentService service) =>
        {
            var ratings = await service.GetRatingsAsync(postId);
            return Results.Ok(ratings);
        }).WithName("GetContentRatings");
    }
}
