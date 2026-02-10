using BasecampSocial.Api.Models.Users;
using BasecampSocial.Api.Services;

namespace BasecampSocial.Api.Endpoints;

/// <summary>Maps user profile endpoints.</summary>
public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/me", async (HttpContext http, IUserService users) =>
        {
            var userId = http.User.GetUserId();
            var profile = await users.GetProfileAsync(userId);
            return Results.Ok(profile);
        })
        .WithName("GetCurrentUser")
        .WithSummary("Get the current user's profile");

        group.MapPatch("/me", async (HttpContext http, UpdateProfileRequest request, IUserService users) =>
        {
            var userId = http.User.GetUserId();
            var profile = await users.UpdateProfileAsync(userId, request);
            return Results.Ok(profile);
        })
        .WithName("UpdateCurrentUser")
        .WithSummary("Update the current user's profile");

        group.MapGet("/{id:guid}", async (Guid id, IUserService users) =>
        {
            var profile = await users.GetProfileAsync(id);
            return Results.Ok(profile);
        })
        .WithName("GetUserById")
        .WithSummary("Get a user's public profile");

        group.MapGet("/search", async (string q, IUserService users) =>
        {
            var results = await users.SearchUsersAsync(q);
            return Results.Ok(results);
        })
        .WithName("SearchUsers")
        .WithSummary("Search users by username or display name");

        return group;
    }
}
