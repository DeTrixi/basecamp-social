namespace BasecampSocial.Api.Models.Users;

/// <summary>Full user profile response for GET /api/v1/users/me and /api/v1/users/:id.</summary>
public sealed record UserProfileResponse(
    Guid Id,
    string UserName,
    string DisplayName,
    string? AvatarUrl,
    string? StatusMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt);
