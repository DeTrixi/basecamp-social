using BasecampSocial.Api.Models.Conversations;
using BasecampSocial.Api.Services;

namespace BasecampSocial.Api.Endpoints;

/// <summary>Maps conversation CRUD and membership endpoints.</summary>
public static class ConversationEndpoints
{
    public static RouteGroupBuilder MapConversationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/conversations")
            .WithTags("Conversations")
            .RequireAuthorization();

        group.MapPost("/", async (HttpContext http, CreateConversationRequest request, IConversationService conversations) =>
        {
            var userId = http.User.GetUserId();
            var result = await conversations.CreateAsync(userId, request);
            return Results.Created($"/api/v1/conversations/{result.Id}", result);
        })
        .WithName("CreateConversation")
        .WithSummary("Create a 1-on-1 or group conversation");

        group.MapGet("/", async (HttpContext http, IConversationService conversations) =>
        {
            var userId = http.User.GetUserId();
            var result = await conversations.ListAsync(userId);
            return Results.Ok(result);
        })
        .WithName("ListConversations")
        .WithSummary("List the current user's conversations");

        group.MapGet("/{id:guid}", async (HttpContext http, Guid id, IConversationService conversations) =>
        {
            var userId = http.User.GetUserId();
            var result = await conversations.GetByIdAsync(userId, id);
            return Results.Ok(result);
        })
        .WithName("GetConversation")
        .WithSummary("Get conversation details and members");

        group.MapPatch("/{id:guid}", async (HttpContext http, Guid id, UpdateConversationRequest request, IConversationService conversations) =>
        {
            var userId = http.User.GetUserId();
            var result = await conversations.UpdateAsync(userId, id, request);
            return Results.Ok(result);
        })
        .WithName("UpdateConversation")
        .WithSummary("Update group name or avatar");

        group.MapPost("/{id:guid}/members", async (HttpContext http, Guid id, AddMembersRequest request, IConversationService conversations) =>
        {
            var userId = http.User.GetUserId();
            await conversations.AddMembersAsync(userId, id, request);
            return Results.NoContent();
        })
        .WithName("AddMembers")
        .WithSummary("Add members to a group conversation");

        group.MapDelete("/{id:guid}/members/{memberUserId:guid}", async (HttpContext http, Guid id, Guid memberUserId, IConversationService conversations) =>
        {
            var userId = http.User.GetUserId();
            await conversations.RemoveMemberAsync(userId, id, memberUserId);
            return Results.NoContent();
        })
        .WithName("RemoveMember")
        .WithSummary("Remove a member from a conversation");

        return group;
    }
}
