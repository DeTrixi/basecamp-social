using BasecampSocial.Api.Models.Keys;
using BasecampSocial.Api.Services;

namespace BasecampSocial.Api.Endpoints;

/// <summary>Maps E2EE key bundle endpoints.</summary>
public static class KeyEndpoints
{
    public static RouteGroupBuilder MapKeyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/keys")
            .WithTags("Keys")
            .RequireAuthorization();

        group.MapPost("/bundle", async (HttpContext http, KeyBundleRequest request, IKeyService keys) =>
        {
            var userId = http.User.GetUserId();
            var result = await keys.UploadBundleAsync(userId, request);
            return Results.Created($"/api/v1/keys/{userId}/bundle", result);
        })
        .WithName("UploadKeyBundle")
        .WithSummary("Upload a pre-key bundle for E2EE key exchange");

        group.MapGet("/{userId:guid}/bundle", async (Guid userId, IKeyService keys) =>
        {
            var result = await keys.GetBundleAsync(userId);
            return Results.Ok(result);
        })
        .WithName("GetKeyBundle")
        .WithSummary("Fetch a user's pre-key bundle");

        return group;
    }
}
