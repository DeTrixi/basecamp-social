using System.Security.Claims;

namespace BasecampSocial.Api.Endpoints;

/// <summary>Helper to extract the current user's ID from the ClaimsPrincipal.</summary>
internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User ID claim not found.");

        return Guid.Parse(sub);
    }
}
