using BasecampSocial.Api.Models.Polls;
using BasecampSocial.Api.Services;

namespace BasecampSocial.Api.Endpoints;

/// <summary>Maps date poll endpoints.</summary>
public static class PollEndpoints
{
    public static RouteGroupBuilder MapPollEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/polls")
            .WithTags("Polls")
            .RequireAuthorization();

        group.MapPost("/", async (HttpContext http, CreatePollRequest request, IPollService polls) =>
        {
            var userId = http.User.GetUserId();
            var result = await polls.CreateAsync(userId, request);
            return Results.Created($"/api/v1/polls/{result.Id}", result);
        })
        .WithName("CreatePoll")
        .WithSummary("Create a date poll in a conversation");

        group.MapGet("/{pollId:guid}", async (HttpContext http, Guid pollId, IPollService polls) =>
        {
            var userId = http.User.GetUserId();
            var result = await polls.GetByIdAsync(userId, pollId);
            return Results.Ok(result);
        })
        .WithName("GetPoll")
        .WithSummary("Get poll details and current votes");

        group.MapPost("/{pollId:guid}/vote", async (HttpContext http, Guid pollId, VoteRequest request, IPollService polls) =>
        {
            var userId = http.User.GetUserId();
            var result = await polls.VoteAsync(userId, pollId, request);
            return Results.Ok(result);
        })
        .WithName("VotePoll")
        .WithSummary("Submit or update your vote on a poll option");

        group.MapDelete("/{pollId:guid}", async (HttpContext http, Guid pollId, IPollService polls) =>
        {
            var userId = http.User.GetUserId();
            await polls.DeleteAsync(userId, pollId);
            return Results.NoContent();
        })
        .WithName("DeletePoll")
        .WithSummary("Close or delete a poll (creator only)");

        group.MapPatch("/{pollId:guid}/finalize", async (HttpContext http, Guid pollId, FinalizePollRequest request, IPollService polls) =>
        {
            var userId = http.User.GetUserId();
            var result = await polls.FinalizeAsync(userId, pollId, request);
            return Results.Ok(result);
        })
        .WithName("FinalizePoll")
        .WithSummary("Lock the poll and set the chosen date");

        return group;
    }
}
