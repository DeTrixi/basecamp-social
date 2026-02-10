using BasecampSocial.Api.Models.Upload;
using BasecampSocial.Api.Services;

namespace BasecampSocial.Api.Endpoints;

/// <summary>Maps file upload endpoints.</summary>
public static class UploadEndpoints
{
    public static RouteGroupBuilder MapUploadEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/upload")
            .WithTags("Upload")
            .RequireAuthorization();

        group.MapPost("/presign", async (HttpContext http, PresignRequest request, IUploadService uploads) =>
        {
            var userId = http.User.GetUserId();
            var result = await uploads.GeneratePresignedUrlAsync(userId, request);
            return Results.Ok(result);
        })
        .WithName("GeneratePresignedUrl")
        .WithSummary("Get a pre-signed URL for encrypted file upload");

        return group;
    }
}
