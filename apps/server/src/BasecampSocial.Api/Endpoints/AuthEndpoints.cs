using BasecampSocial.Api.Models.Auth;
using BasecampSocial.Api.Services;

namespace BasecampSocial.Api.Endpoints;

/// <summary>Maps auth endpoints: register, login, refresh.</summary>
public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth")
            .RequireRateLimiting("auth");

        group.MapPost("/register", async (RegisterRequest request, IAuthService auth) =>
        {
            var result = await auth.RegisterAsync(request);
            return Results.Created($"/api/v1/users/{result.User.Id}", result);
        })
        .AllowAnonymous()
        .WithName("Register")
        .WithSummary("Register a new account");

        group.MapPost("/login", async (LoginRequest request, IAuthService auth) =>
        {
            var result = await auth.LoginAsync(request);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("Login")
        .WithSummary("Login and receive JWT tokens");

        group.MapPost("/refresh", async (RefreshRequest request, IAuthService auth) =>
        {
            var result = await auth.RefreshAsync(request);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("RefreshToken")
        .WithSummary("Refresh an expired access token");

        return group;
    }
}
