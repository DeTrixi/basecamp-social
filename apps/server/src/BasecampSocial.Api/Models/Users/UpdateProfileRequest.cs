namespace BasecampSocial.Api.Models.Users;

/// <summary>Request body for PATCH /api/v1/users/me.</summary>
public sealed record UpdateProfileRequest(
    string? DisplayName,
    string? AvatarUrl,
    string? StatusMessage);
