using BasecampSocial.Api.Models.Messages;
using BasecampSocial.Api.Services;

namespace BasecampSocial.Api.Endpoints;

/// <summary>Maps encrypted message endpoints.</summary>
public static class MessageEndpoints
{
    public static RouteGroupBuilder MapMessageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/messages")
            .WithTags("Messages")
            .RequireAuthorization();

        group.MapGet("/{conversationId:guid}", async (
            HttpContext http,
            Guid conversationId,
            IMessageService messages,
            DateTimeOffset? before,
            int limit = 50) =>
        {
            var userId = http.User.GetUserId();
            var result = await messages.GetMessagesAsync(userId, conversationId, before, limit);
            return Results.Ok(result);
        })
        .WithName("GetMessages")
        .WithSummary("Fetch encrypted messages (paginated, newest first)");

        group.MapPost("/{conversationId:guid}/read", async (
            HttpContext http,
            Guid conversationId,
            ReadMessagesRequest request,
            IMessageService messages) =>
        {
            var userId = http.User.GetUserId();
            await messages.MarkAsReadAsync(userId, conversationId, request);
            return Results.NoContent();
        })
        .WithName("MarkMessagesRead")
        .WithSummary("Mark messages as read in a conversation");

        return group;
    }
}
